using EntertainmentCenter.ViewModels;

namespace EntertainmentCenter.Views;

public partial class AdminDashboardPage : ContentPage
{
    private readonly AdminDashboardViewModel _vm;

    public AdminDashboardPage(AdminDashboardViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadMetricsCommand.ExecuteAsync(null);
    }
}
