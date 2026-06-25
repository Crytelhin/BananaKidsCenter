using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EntertainmentCenter.Services;
using EntertainmentCenter.Resources.Strings;

namespace EntertainmentCenter.ViewModels;

public partial class ZoneEditViewModel : ObservableObject, IQueryAttributable
{
    private readonly ZoneApiService _zoneService;

    private int _zoneId;

    [ObservableProperty]
    private string zoneName = "";

    [ObservableProperty]
    private bool isActive = true;

    [ObservableProperty]
    private ObservableCollection<TariffEdit> tariffs = [];

    [ObservableProperty]
    private bool isTariffsEmpty = true;

    [ObservableProperty]
    private bool isBusy;

    public ZoneEditViewModel(ZoneApiService zoneService)
    {
        _zoneService = zoneService;
    }

    private void UpdateEmptyState()
    {
        IsTariffsEmpty = Tariffs == null || Tariffs.Count == 0;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("zoneId", out var idObj) && int.TryParse(idObj?.ToString(), out var id))
        {
            _zoneId = id;
            _ = LoadExistingZone();
        }
    }

    private async Task LoadExistingZone()
    {
        IsBusy = true;
        try
        {
            var zones = await _zoneService.GetAllAsync(true);
            var zone = zones?.FirstOrDefault(z => z.Id == _zoneId);
            if (zone != null)
            {
                ZoneName = zone.Name;
                IsActive = zone.IsActive;
                Tariffs = new ObservableCollection<TariffEdit>(
                    zone.Tariffs.Where(t => t.IsActive).Select(TariffEdit.FromTariff));
                UpdateEmptyState();
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
    private void AddTariff()
    {
        Tariffs.Add(new TariffEdit
        {
            ZoneId = _zoneId,
            Label = "",
            DurationHours = 1,
            DurationMinutes = 0,
            Price = 0,
            IsActive = true
        });
        UpdateEmptyState();
    }

    [RelayCommand]
    private async Task RemoveTariff(TariffEdit tariff)
    {
        try
        {
            if (tariff.Id > 0)
                await _zoneService.DeleteTariffAsync(tariff.Id);

            Tariffs.Remove(tariff);
            UpdateEmptyState();
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
    private async Task Save()
    {
        // Validate zone name
        if (string.IsNullOrWhiteSpace(ZoneName))
        {
            await Shell.Current.DisplayAlert(AppResources.Error, AppResources.EnterZoneName, "OK");
            return;
        }

        // Validate tariffs
        for (int i = 0; i < Tariffs.Count; i++)
        {
            var t = Tariffs[i];
            if (string.IsNullOrWhiteSpace(t.Label))
            {
                await Shell.Current.DisplayAlert(AppResources.Error, string.Format(AppResources.EnterTariffNameFormat, i + 1), "OK");
                return;
            }
            if (t.DurationHours <= 0 && t.DurationMinutes <= 0)
            {
                await Shell.Current.DisplayAlert(AppResources.Error, string.Format(AppResources.EnterTariffDurationFormat, t.Label), "OK");
                return;
            }
            if (t.Price <= 0)
            {
                await Shell.Current.DisplayAlert(AppResources.Error, string.Format(AppResources.EnterTariffPriceFormat, t.Label), "OK");
                return;
            }
        }

        IsBusy = true;
        try
        {
            var zone = new Models.Zone
            {
                Id = _zoneId,
                Name = ZoneName,
                IsActive = IsActive
            };

            var savedZone = await _zoneService.SaveZoneAsync(zone);
            if (savedZone == null)
            {
                await Shell.Current.DisplayAlert(AppResources.Error, AppResources.ServerError, "OK");
                return;
            }

            foreach (var tariff in Tariffs)
            {
                tariff.ZoneId = savedZone.Id;
                await _zoneService.SaveTariffAsync(savedZone.Id, tariff.ToTariff());
            }

            await Shell.Current.GoToAsync("..");
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
