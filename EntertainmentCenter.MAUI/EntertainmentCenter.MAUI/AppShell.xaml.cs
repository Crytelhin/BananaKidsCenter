using EntertainmentCenter.Views;

namespace EntertainmentCenter;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Main role pages
        Routing.RegisterRoute("ReceptionPage", typeof(ReceptionPage));
        Routing.RegisterRoute("EntryCheckPage", typeof(EntryCheckPage));

        // Admin flow
        Routing.RegisterRoute("AdminPinPage", typeof(AdminPinPage));
        Routing.RegisterRoute("AdminDashboardPage", typeof(AdminDashboardPage));
        Routing.RegisterRoute("ZonesListPage", typeof(ZonesListPage));
        Routing.RegisterRoute("PromotionsListPage", typeof(PromotionsListPage));

        // Client flow
        Routing.RegisterRoute("AddClientPage", typeof(AddClientPage));
        Routing.RegisterRoute("ClientDetailPage", typeof(ClientDetailPage));
        Routing.RegisterRoute("ClientHistoryPage", typeof(ClientHistoryPage));

        // Settings & config
        Routing.RegisterRoute("SettingsPage", typeof(SettingsPage));
        Routing.RegisterRoute("NotificationSettingsPage", typeof(NotificationSettingsPage));
        Routing.RegisterRoute("ServerConnectionPage", typeof(ServerConnectionPage));

        // Edit pages
        Routing.RegisterRoute("ZoneEditPage", typeof(ZoneEditPage));
        Routing.RegisterRoute("PromotionEditPage", typeof(PromotionEditPage));

    }
}
