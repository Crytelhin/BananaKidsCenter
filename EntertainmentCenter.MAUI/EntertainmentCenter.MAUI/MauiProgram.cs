using EntertainmentCenter.Services;
using EntertainmentCenter.ViewModels;
using EntertainmentCenter.Views;
using Microsoft.Extensions.Logging;

namespace EntertainmentCenter;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Services
        builder.Services.AddSingleton<ServerAddressManager>();
        builder.Services.AddSingleton<IServerDiscoveryService, ServerDiscoveryService>();
        builder.Services.AddTransient<DiscoveryHttpMessageHandler>();
        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<ZoneApiService>();
        builder.Services.AddSingleton<PromotionApiService>();
        builder.Services.AddSingleton<ClientApiService>();
        builder.Services.AddSingleton<SessionApiService>();
        builder.Services.AddSingleton<AdminApiService>();
        builder.Services.AddSingleton<GlobalNotificationService>();
        builder.Services.AddSingleton<IBarcodeScannerService, BarcodeScannerService>();

        // ViewModels — new
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<AdminPinViewModel>();
        builder.Services.AddTransient<AdminDashboardViewModel>();
        builder.Services.AddTransient<ZonesListViewModel>();
        builder.Services.AddTransient<PromotionsListViewModel>();
        builder.Services.AddTransient<ClientHistoryViewModel>();
        builder.Services.AddTransient<ClientDetailViewModel>();
        builder.Services.AddTransient<NotificationSettingsViewModel>();
        builder.Services.AddTransient<ServerConnectionViewModel>();

        // ViewModels — existing
        builder.Services.AddTransient<ReceptionViewModel>();
        builder.Services.AddTransient<AddClientViewModel>();
        builder.Services.AddTransient<EntryCheckViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<ZoneEditViewModel>();
        builder.Services.AddTransient<PromotionEditViewModel>();

        // Pages — new
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<AdminPinPage>();
        builder.Services.AddTransient<AdminDashboardPage>();
        builder.Services.AddTransient<ZonesListPage>();
        builder.Services.AddTransient<PromotionsListPage>();
        builder.Services.AddTransient<ClientHistoryPage>();
        builder.Services.AddTransient<ClientDetailPage>();
        builder.Services.AddTransient<NotificationSettingsPage>();
        builder.Services.AddTransient<ServerConnectionPage>();

        // Pages — existing
        builder.Services.AddTransient<ReceptionPage>();
        builder.Services.AddTransient<EntryCheckPage>();
        builder.Services.AddTransient<AddClientPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<ZoneEditPage>();
        builder.Services.AddTransient<PromotionEditPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
