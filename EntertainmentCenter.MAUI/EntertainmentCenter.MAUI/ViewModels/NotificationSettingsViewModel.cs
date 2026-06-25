using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EntertainmentCenter.Services;
using EntertainmentCenter.Resources.Strings;

namespace EntertainmentCenter.ViewModels;

public partial class NotificationSettingsViewModel : ObservableObject
{
    private readonly AdminApiService _adminService;

    [ObservableProperty]
    private bool warningEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WarningInfoText))]
    private int warningMinutesBeforeExpiry = 5;

    public string WarningInfoText => string.Format(AppResources.NotificationWarningStringFormat, WarningMinutesBeforeExpiry);

    [ObservableProperty]
    private bool isBusy;

    public NotificationSettingsViewModel(AdminApiService adminService)
    {
        _adminService = adminService;
    }

    [RelayCommand]
    private async Task LoadSettings()
    {
        IsBusy = true;
        try
        {
            var settings = await _adminService.GetNotificationSettingsAsync();
            if (settings != null)
            {
                WarningEnabled = settings.WarningEnabled;
                WarningMinutesBeforeExpiry = settings.WarningMinutesBeforeExpiry;
            }
        }
        catch (HttpRequestException)
        {
            await Shell.Current.DisplayAlert(AppResources.Error, AppResources.NoConnectionToServer, "OK");
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlert(AppResources.Error, AppResources.ServerError, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveSettings()
    {
        IsBusy = true;
        try
        {
            await _adminService.UpdateNotificationSettingsAsync(WarningEnabled, WarningMinutesBeforeExpiry);
            await Shell.Current.DisplayAlert(AppResources.Saved, AppResources.NotificationSettingsSaved, "OK");
        }
        catch (HttpRequestException)
        {
            await Shell.Current.DisplayAlert(AppResources.Error, AppResources.NoConnectionToServer, "OK");
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlert(AppResources.Error, AppResources.ServerError, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
