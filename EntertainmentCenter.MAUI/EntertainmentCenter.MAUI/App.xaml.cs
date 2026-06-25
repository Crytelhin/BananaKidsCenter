using EntertainmentCenter.Services;

namespace EntertainmentCenter
{
    public partial class App : Application
    {
        public App(GlobalNotificationService globalNotificationService, IServerDiscoveryService serverDiscoveryService)
        {
            var lang = Preferences.Get("AppLanguage", "Русский");
            var culture = lang switch
            {
                "Română" => new System.Globalization.CultureInfo("ro"),
                "English" => new System.Globalization.CultureInfo("en"),
                _ => new System.Globalization.CultureInfo("ru")
            };
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;

            InitializeComponent();
            globalNotificationService.Start();
            serverDiscoveryService.StartPassiveListener();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}