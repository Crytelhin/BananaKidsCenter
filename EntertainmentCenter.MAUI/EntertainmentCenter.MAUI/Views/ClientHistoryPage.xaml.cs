using EntertainmentCenter.ViewModels;

namespace EntertainmentCenter.Views;

public partial class ClientHistoryPage : ContentPage
{
    private readonly ClientHistoryViewModel _vm;

    public ClientHistoryPage(ClientHistoryViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.SearchCommand.ExecuteAsync(null);
    }
}
