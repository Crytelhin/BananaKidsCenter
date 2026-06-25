using EntertainmentCenter.ViewModels;

namespace EntertainmentCenter.Views;

public partial class ServerConnectionPage : ContentPage
{
    public ServerConnectionPage(ServerConnectionViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
