using EntertainmentCenter.ViewModels;

namespace EntertainmentCenter.Views;

public partial class ZonesListPage : ContentPage
{
    private readonly ZonesListViewModel _vm;

    public ZonesListPage(ZonesListViewModel vm)
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
