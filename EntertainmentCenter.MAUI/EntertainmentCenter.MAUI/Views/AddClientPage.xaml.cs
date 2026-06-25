using EntertainmentCenter.ViewModels;

namespace EntertainmentCenter.Views;

public partial class AddClientPage : ContentPage
{
    public AddClientPage(AddClientViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var vm = BindingContext as AddClientViewModel;
        await vm!.LoadDataCommand.ExecuteAsync(null);
    }
}
