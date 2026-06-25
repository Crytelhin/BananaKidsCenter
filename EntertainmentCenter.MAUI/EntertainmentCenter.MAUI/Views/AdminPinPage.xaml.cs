using EntertainmentCenter.ViewModels;

namespace EntertainmentCenter.Views;

public partial class AdminPinPage : ContentPage
{
    public AdminPinPage(AdminPinViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
