using EntertainmentCenter.ViewModels;

namespace EntertainmentCenter.Views;

public partial class NotificationSettingsPage : ContentPage
{
    private readonly NotificationSettingsViewModel _vm;

    public NotificationSettingsPage(NotificationSettingsViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadSettingsCommand.ExecuteAsync(null);
    }
}
