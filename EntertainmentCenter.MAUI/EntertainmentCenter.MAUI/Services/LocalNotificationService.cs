using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using EntertainmentCenter.Resources.Strings;

#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
#endif

namespace EntertainmentCenter.Services
{
    public static class LocalNotificationService
    {
        public static async Task RequestPermissionAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                if (status != PermissionStatus.Granted)
                {
                    await Permissions.RequestAsync<Permissions.PostNotifications>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to request notification permission: {ex.Message}");
            }
        }

        public static void ShowNotification(string title, string message)
        {
#if ANDROID
            try
            {
                var context = Platform.CurrentActivity ?? Platform.AppContext;
                if (context == null) return;

                var channelId = "entertainment_center_warnings";
                var notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;

                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    // Delete existing channel first so updates take effect
                    notificationManager?.DeleteNotificationChannel(channelId);

                    var channel = new NotificationChannel(channelId, AppResources.NotificationChannelName, NotificationImportance.High)
                    {
                        Description = AppResources.NotificationChannelDescription
                    };
                    channel.EnableVibration(true);
                    channel.EnableLights(true);
                    // Explicit vibration pattern: 500ms vibrate, 300ms pause, 500ms vibrate
                    channel.SetVibrationPattern([500, 300, 500]);
                    // Explicit sound — use default notification sound
                    var soundUri = Android.Provider.Settings.System.DefaultNotificationUri;
                    if (soundUri != null)
                    {
                        channel.SetSound(soundUri, null);
                    }

                    notificationManager?.CreateNotificationChannel(channel);
                }

                // Create intent to open app when clicking notification
                var pm = context.PackageManager;
                var packageName = context.PackageName;
                var intent = !string.IsNullOrEmpty(packageName) ? pm?.GetLaunchIntentForPackage(packageName) : null;
                if (intent == null)
                {
                    intent = new Intent(context, typeof(MainActivity));
                }
                intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
                
                var pendingIntentFlags = Build.VERSION.SdkInt >= BuildVersionCodes.S 
                    ? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable 
                    : PendingIntentFlags.UpdateCurrent;

                var pendingIntent = PendingIntent.GetActivity(context, 0, intent, pendingIntentFlags);

                int iconId = context.ApplicationInfo?.Icon ?? Android.Resource.Drawable.SymDefAppIcon;

                var builder = new Android.App.Notification.Builder(context, channelId)
                    .SetContentTitle(title)
                    .SetContentText(message)
                    .SetSmallIcon(iconId)
                    .SetContentIntent(pendingIntent)
                    .SetAutoCancel(true);

                // Add sound and vibration explicitly for maximum device compatibility
                builder.SetDefaults(Android.App.NotificationDefaults.All);
                builder.SetVibrate([500, 300, 500]);
                var defaultSound = Android.Provider.Settings.System.DefaultNotificationUri;
                if (defaultSound != null)
                {
                    builder.SetSound(defaultSound);
                }

                var notificationId = new Random().Next(1000, 9999);
                notificationManager?.Notify(notificationId, builder.Build());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show native notification: {ex.Message}");
            }
#endif
        }
    }
}
