using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EntertainmentCenter.Models;
using EntertainmentCenter.Services;
using EntertainmentCenter.Resources.Strings;

namespace EntertainmentCenter.ViewModels;

public partial class ZonesListViewModel : ObservableObject
{
    private readonly ZoneApiService _zoneService;

    [ObservableProperty]
    private ObservableCollection<ZoneDisplay> zones = [];

    [ObservableProperty]
    private bool isBusy;

    public ZonesListViewModel(ZoneApiService zoneService)
    {
        _zoneService = zoneService;
    }

    [RelayCommand]
    private async Task LoadData()
    {
        IsBusy = true;
        try
        {
            var result = await _zoneService.GetAllAsync(true);
            Zones = new ObservableCollection<ZoneDisplay>(
                (result ?? []).Select(z => new ZoneDisplay
                {
                    Id = z.Id,
                    Name = z.Name,
                    IsActive = z.IsActive,
                    TariffCount = z.Tariffs?.Count(t => t.IsActive) ?? 0
                }));
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
    private async Task ToggleZone(ZoneDisplay zone)
    {
        try
        {
            // Quick toggle: save zone with flipped IsActive
            var z = new Zone
            {
                Id = zone.Id,
                Name = zone.Name,
                IsActive = !zone.IsActive
            };
            var saved = await _zoneService.SaveZoneAsync(z);
            if (saved != null)
                zone.IsActive = saved.IsActive;
        }
        catch (HttpRequestException)
        {
            await Shell.Current.DisplayAlert(AppResources.Error, AppResources.NoConnectionToServer, "OK");
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlert(AppResources.Error, AppResources.ServerError, "OK");
        }
    }

    [RelayCommand]
    private async Task EditZone(ZoneDisplay zone)
    {
        await Shell.Current.GoToAsync($"ZoneEditPage?zoneId={zone.Id}");
    }

    [RelayCommand]
    private async Task AddZone()
    {
        await Shell.Current.GoToAsync("ZoneEditPage");
    }

    [RelayCommand]
    private async Task DeleteZone(ZoneDisplay zone)
    {
        bool confirm = await Shell.Current.DisplayAlert(
            AppResources.DeleteTitle,
            string.Format(AppResources.DeleteZoneConfirm, zone.Name),
            AppResources.DeleteButton, AppResources.CancelButton);

        if (!confirm) return;

        IsBusy = true;
        try
        {
            await _zoneService.DeleteZoneAsync(zone.Id);
            await LoadData();
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

public partial class ZoneDisplay : ObservableObject
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int TariffCount { get; set; }
    public bool IsActive { get; set; } = true;

    public string TariffCountText => string.Format(AppResources.TariffCountFormat, TariffCount);
}
