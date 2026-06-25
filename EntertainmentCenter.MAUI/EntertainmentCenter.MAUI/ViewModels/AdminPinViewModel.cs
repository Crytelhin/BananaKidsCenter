using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EntertainmentCenter.Services;

namespace EntertainmentCenter.ViewModels;

public partial class AdminPinViewModel : ObservableObject
{
    private readonly AdminApiService _adminService;

    [ObservableProperty]
    private string dots = "····";

    [ObservableProperty]
    private bool isError;

    [ObservableProperty]
    private bool isBusy;

    private string _pin = "";

    public AdminPinViewModel(AdminApiService adminService)
    {
        _adminService = adminService;
    }

    [RelayCommand]
    private void AddDigit(string digit)
    {
        if (IsError)
        {
            IsError = false;
            Dots = "····";
            _pin = "";
        }

        if (_pin.Length >= 4) return;

        _pin += digit;
        UpdateDots();

        if (_pin.Length == 4)
            _ = Submit();
    }

    [RelayCommand]
    private void ClearLast()
    {
        if (_pin.Length > 0)
        {
            _pin = _pin[..^1];
            UpdateDots();
        }
    }

    private void UpdateDots()
    {
        var chars = new char[4];
        for (int i = 0; i < 4; i++)
            chars[i] = i < _pin.Length ? '●' : '·';
        Dots = new string(chars);
    }

    private async Task Submit()
    {
        IsBusy = true;
        try
        {
            var result = await _adminService.VerifyPinAsync(_pin);
            if (result)
            {
                NavigateToAdmin();
                return;
            }
            await ShowError();
        }
        catch
        {
            // If API is unreachable, fall back to checking against the default PIN
            // so staff can still access settings to change the server IP
            if (_pin == "1234")
            {
                NavigateToAdmin();
                return;
            }
            await ShowError();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void NavigateToAdmin()
    {
        _pin = "";
        Dots = "····";
        IsError = false;
        IsBusy = false;
        await Shell.Current.GoToAsync("AdminDashboardPage");
    }

    private async Task ShowError()
    {
        IsError = true;
        _pin = "";
        Dots = "····";
        await Task.Delay(800);
        IsError = false;
        Dots = "····";
    }
}
