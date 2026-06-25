using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EntertainmentCenter.Messages;
using EntertainmentCenter.Models;
using EntertainmentCenter.Services;
using EntertainmentCenter.Resources.Strings;

namespace EntertainmentCenter.ViewModels;

public partial class AddClientViewModel : ObservableObject
{
    private readonly ZoneApiService _zoneService;
    private readonly PromotionApiService _promotionService;
    private readonly ClientApiService _clientService;
    private readonly SessionApiService _sessionService;
    private readonly IBarcodeScannerService _barcodeScannerService;

    [ObservableProperty]
    private string fullName = "";

    [ObservableProperty]
    private string phone = "373";

    [ObservableProperty]
    private string cardCode = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPhysicalCard))]
    private bool isCardless;

    public bool IsPhysicalCard => !IsCardless;

    [ObservableProperty]
    private bool isVip;

    [ObservableProperty]
    private bool acceptsMarketing;

    [ObservableProperty]
    private ObservableCollection<Zone> zones = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FinalPrice))]
    private Zone? selectedZone;

    [ObservableProperty]
    private ObservableCollection<Tariff> tariffs = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FinalPrice))]
    private Tariff? selectedTariff;

    [ObservableProperty]
    private ObservableCollection<Promotion> promotions = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FinalPrice))]
    private Promotion? selectedPromotion;

    [ObservableProperty]
    private decimal finalPrice;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FinalPrice))]
    private string customHours = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FinalPrice))]
    private string customMinutes = "";

    [ObservableProperty]
    private bool isBusy;

    public AddClientViewModel(
        ZoneApiService zoneService,
        PromotionApiService promotionService,
        ClientApiService clientService,
        SessionApiService sessionService,
        IBarcodeScannerService barcodeScannerService)
    {
        _zoneService = zoneService;
        _promotionService = promotionService;
        _clientService = clientService;
        _sessionService = sessionService;
        _barcodeScannerService = barcodeScannerService;
    }

    [RelayCommand]
    private async Task LoadData()
    {
        IsBusy = true;
        try
        {
            var zonesResult = await _zoneService.GetAllWithTariffsAsync();
            Zones = new ObservableCollection<Zone>(zonesResult ?? []);

            var promosResult = await _promotionService.GetActiveAsync();
            Promotions = new ObservableCollection<Promotion>(promosResult ?? []);
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

    partial void OnSelectedZoneChanged(Zone? value)
    {
        Tariffs = new ObservableCollection<Tariff>(value?.Tariffs ?? []);
        SelectedTariff = null;
    }

    partial void OnSelectedTariffChanged(Tariff? value)
    {
        if (value != null)
        {
            var totalMinutes = (int)value.Duration.TotalMinutes;
            CustomHours = (totalMinutes / 60).ToString();
            CustomMinutes = (totalMinutes % 60).ToString();
        }
        RecalculateFinalPrice();
    }

    partial void OnSelectedPromotionChanged(Promotion? value) => RecalculateFinalPrice();
    partial void OnCustomHoursChanged(string value) => RecalculateFinalPrice();
    partial void OnCustomMinutesChanged(string value) => RecalculateFinalPrice();

    private int GetCustomDurationMinutes()
    {
        var hours = int.TryParse(CustomHours, out var h) ? h : 0;
        var minutes = int.TryParse(CustomMinutes, out var m) ? m : 0;
        return Math.Max(1, hours * 60 + minutes);
    }

    private void RecalculateFinalPrice()
    {
        if (SelectedTariff == null)
        {
            FinalPrice = 0;
            return;
        }

        var customMinutes = GetCustomDurationMinutes();
        var tariffMinutes = SelectedTariff.Duration.TotalMinutes;
        var price = tariffMinutes > 0
            ? SelectedTariff.Price * ((decimal)customMinutes / (decimal)tariffMinutes)
            : SelectedTariff.Price;

        if (SelectedPromotion != null)
        {
            if (SelectedPromotion.DiscountType == DiscountType.Percent)
                price -= price * (SelectedPromotion.DiscountValue / 100);
            else
                price -= SelectedPromotion.DiscountValue;
        }

        FinalPrice = Math.Round(Math.Max(0, price), 2);
    }

    [RelayCommand]
    private async Task ScanBarcode()
    {
        IsBusy = true;
        try
        {
            var code = await _barcodeScannerService.ScanAsync();
            if (!string.IsNullOrWhiteSpace(code))
            {
                CardCode = code;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SetCardlessMode(bool mode)
    {
        IsCardless = mode;
    }

    [RelayCommand]
    private async Task AddClient()
    {
        if (string.IsNullOrWhiteSpace(FullName))
        {
            await Shell.Current.DisplayAlert(AppResources.Error, AppResources.FillFullName, "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(Phone))
        {
            await Shell.Current.DisplayAlert(AppResources.Error, AppResources.FillPhone, "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(CardCode) && !IsCardless)
        {
            await Shell.Current.DisplayAlert(AppResources.Error, AppResources.FillCard, "OK");
            return;
        }
        if (SelectedTariff == null)
        {
            await Shell.Current.DisplayAlert(AppResources.Error, AppResources.SelectTariff, "OK");
            return;
        }

        IsBusy = true;
        try
        {
            var finalCardCode = IsCardless
                ? "nocard_" + Guid.NewGuid().ToString("N").Substring(0, 8)
                : CardCode;

            var client = new Client
            {
                FullName = FullName,
                Phone = Phone,
                CardCode = finalCardCode,
                IsVip = IsVip,
                AcceptsMarketing = AcceptsMarketing
            };

            var created = await _clientService.AddAsync(client);
            if (created == null)
            {
                await Shell.Current.DisplayAlert(AppResources.Error, AppResources.ServerError, "OK");
                return;
            }

            var customMinutes = GetCustomDurationMinutes();
            var tariffMinutes = (int)SelectedTariff.Duration.TotalMinutes;
            var effectiveMinutes = customMinutes != tariffMinutes ? customMinutes : (int?)null;
            await _sessionService.StartSessionAsync(created.Id, SelectedTariff.Id, SelectedPromotion?.Id, effectiveMinutes, IsCardless);

            await Shell.Current.DisplayAlert(AppResources.Success, string.Format(AppResources.ClientRegisteredSuccess, client.FullName), "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (InvalidOperationException ex)
        {
            await Shell.Current.DisplayAlert(AppResources.Error, ex.Message, "OK");
        }
        catch (HttpRequestException)
        {
            await Shell.Current.DisplayAlert(AppResources.Error, AppResources.NoConnectionToServer, "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert(AppResources.Error, string.Format(AppResources.ServerErrorWithDetails, ex.Message), "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
