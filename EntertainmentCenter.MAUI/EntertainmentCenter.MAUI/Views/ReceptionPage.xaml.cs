using EntertainmentCenter.ViewModels;

namespace EntertainmentCenter.Views;

public partial class ReceptionPage : ContentPage
{
    public ReceptionPage(ReceptionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var vm = BindingContext as ReceptionViewModel;
        await vm!.LoadActiveSessionsCommand.ExecuteAsync(null);
    }
}
