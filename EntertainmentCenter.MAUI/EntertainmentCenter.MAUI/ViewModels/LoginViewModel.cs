using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EntertainmentCenter.Services;

namespace EntertainmentCenter.ViewModels;

public partial class LoginViewModel : BaseConnectionViewModel
{
    public LoginViewModel(ServerAddressManager addressManager) : base(addressManager)
    {
    }

    [RelayCommand]
    private async Task GoToReception()
    {
        await Shell.Current.GoToAsync("ReceptionPage");
    }

    [RelayCommand]
    private async Task GoToEntryCheck()
    {
        await Shell.Current.GoToAsync("EntryCheckPage");
    }

    [RelayCommand]
    private async Task GoToAdminPin()
    {
        await Shell.Current.GoToAsync("AdminPinPage");
    }
}
