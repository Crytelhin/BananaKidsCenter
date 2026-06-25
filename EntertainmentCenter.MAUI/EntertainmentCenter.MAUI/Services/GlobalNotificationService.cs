using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using EntertainmentCenter.Models;
using EntertainmentCenter.Resources.Strings;

namespace EntertainmentCenter.Services
{
    public class GlobalNotificationService
    {
        private readonly SessionApiService _sessionService;
        private readonly AdminApiService _adminService;
        private bool _isTimerRunning;
        private readonly HashSet<int> _warnedSessionIds = [];

        public GlobalNotificationService(SessionApiService sessionService, AdminApiService adminService)
        {
            _sessionService = sessionService;
            _adminService = adminService;
        }

        public void Start()
        {
            if (_isTimerRunning) return;
            _isTimerRunning = true;

            // Start a dispatcher timer running every 10 seconds
            Application.Current?.Dispatcher.StartTimer(TimeSpan.FromSeconds(10), () =>
            {
                _ = CheckExpiryWarningsAsync();
                return _isTimerRunning; // true to continue, false to stop
            });
        }

        public void Stop()
        {
            _isTimerRunning = false;
        }

        private async Task CheckExpiryWarningsAsync()
        {
            try
            {
                // Fetch settings
                var settings = await _adminService.GetNotificationSettingsAsync();
                if (settings == null || !settings.WarningEnabled) return;

                var threshold = TimeSpan.FromMinutes(settings.WarningMinutesBeforeExpiry);
                var now = DateTime.UtcNow;

                // Fetch active sessions
                var sessions = await _sessionService.GetAllActiveAsync();
                if (sessions == null) return;

                foreach (var session in sessions)
                {
                    if (session.ActivatedAt == null || !session.IsActive) continue;

                    var remaining = session.ExpiresAt - now;
                    if (remaining <= threshold && remaining > TimeSpan.Zero && !_warnedSessionIds.Contains(session.Id))
                    {
                        _warnedSessionIds.Add(session.Id);
                        
                        var clientName = session.Client?.FullName ?? AppResources.ClientDefaultName;
                        var minutes = (int)remaining.TotalMinutes;
                        var minText = minutes > 0 ? $"{minutes} {AppResources.MinuteShort}" : AppResources.LessThanMinute;

                        // Send native notification
                        LocalNotificationService.ShowNotification(
                            AppResources.NotificationTitle,
                            string.Format(AppResources.NotificationBodyFormat, clientName, minText)
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                // Silently ignore HTTP errors or timeouts
                System.Diagnostics.Debug.WriteLine($"Error in background notifications check: {ex.Message}");
            }
        }
    }
}
