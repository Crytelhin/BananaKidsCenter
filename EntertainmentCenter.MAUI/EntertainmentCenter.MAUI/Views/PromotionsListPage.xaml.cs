using EntertainmentCenter.ViewModels;

namespace EntertainmentCenter.Views;

public partial class PromotionsListPage : ContentPage
{
    private readonly PromotionsListViewModel _vm;

    public PromotionsListPage(PromotionsListViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadDataCommand.ExecuteAsync(null);
    }
}
