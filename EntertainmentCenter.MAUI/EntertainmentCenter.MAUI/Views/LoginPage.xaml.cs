using EntertainmentCenter.ViewModels;
using EntertainmentCenter.Services;

namespace EntertainmentCenter.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LocalNotificationService.RequestPermissionAsync();
    }
}
