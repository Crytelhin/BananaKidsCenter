using EntertainmentCenter.ViewModels;

namespace EntertainmentCenter.Views;

public partial class ZoneEditPage : ContentPage
{
    public ZoneEditPage(ZoneEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
