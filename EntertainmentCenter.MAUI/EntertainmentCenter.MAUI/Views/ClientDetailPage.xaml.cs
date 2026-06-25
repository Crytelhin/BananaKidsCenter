using EntertainmentCenter.ViewModels;

namespace EntertainmentCenter.Views;

public partial class ClientDetailPage : ContentPage
{
    public ClientDetailPage(ClientDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
