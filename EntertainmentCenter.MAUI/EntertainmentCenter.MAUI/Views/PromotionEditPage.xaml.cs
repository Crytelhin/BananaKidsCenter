using EntertainmentCenter.ViewModels;

namespace EntertainmentCenter.Views;

public partial class PromotionEditPage : ContentPage
{
    public PromotionEditPage(PromotionEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
