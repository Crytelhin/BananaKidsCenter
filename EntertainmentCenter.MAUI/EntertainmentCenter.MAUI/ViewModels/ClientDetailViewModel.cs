using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EntertainmentCenter.Models;
using EntertainmentCenter.Services;
using EntertainmentCenter.Resources.Strings;

namespace EntertainmentCenter.ViewModels;

public partial class ClientDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly SessionApiService _sessionService;
    private readonly ZoneApiService _zoneService;
    private readonly PromotionApiService _promotionService;
    private IDispatcherTimer? _timer;

    private int _sessionId;

    [ObservableProperty]
    private Session? session;

    [ObservableProperty]
    private string clientName = "";

    [ObservableProperty]
    private string phone = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPhysicalCard))]
    private string cardCode = "";

    public bool IsPhysicalCard => !string.IsNullOrEmpty(CardCode) && !CardCode.StartsWith("nocard");

    [ObservableProperty]
    private string zoneName = "";

    [ObservableProperty]
    private string tariffLabel = "";

    [ObservableProperty]
    private bool isVip;

    [ObservableProperty]
    private string entryTime = "";

    [ObservableProperty]
    private string expiresAt = "";

    [ObservableProperty]
    private string timeRemaining = "00:00";

    [ObservableProperty]
    private bool isActive;

    [ObservableProperty]
    private bool isPending;

    [ObservableProperty]
    private decimal finalPrice;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isExpired;

    [ObservableProperty]
    private bool isExtendPanelVisible;

    [ObservableProperty]
    private ObservableCollection<Tariff> extendTariffs = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExtendPrice))]
    private Tariff? selectedExtendTariff;

    [ObservableProperty]
    private ObservableCollection<Promotion> extendPromotions = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExtendPrice))]
    private Promotion? selectedExtendPromotion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExtendPrice))]
    private string customExtendHours = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExtendPrice))]
    private string customExtendMinutes = "";

    [ObservableProperty]
    private decimal extendPrice;

    public ClientDetailViewModel(
        SessionApiService sessionService,
        ZoneApiService zoneService,
        PromotionApiService promotionService)
    {
        _sessionService = sessionService;
        _zoneService = zoneService;
        _promotionService = promotionService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("sessionId", out var idObj) && int.TryParse(idObj?.ToString(), out var id))
        {
            _sessionId = id;
            _ = LoadSession();
        }
    }

    [RelayCommand]
    private async Task LoadSession()
    {
        IsBusy = true;
        try
        {
            var s = await _sessionService.GetByIdAsync(_sessionId);
            if (s == null)
            {
                await Shell.Current.DisplayAlert(AppResources.Error, AppResources.SessionNotFound, "OK");
                return;
            }

            Session = s;
            ClientName = s.Client?.FullName ?? "";
            Phone = s.Client?.Phone ?? "";
            CardCode = s.Client?.CardCode ?? "";
            ZoneName = s.Tariff?.Zone?.Name ?? "";
            TariffLabel = s.Tariff?.Label ?? "";
            IsVip = s.Client?.IsVip ?? false;
            EntryTime = s.ActivatedAt.HasValue
                ? s.ActivatedAt.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
                : AppResources.PendingEntry;

            ExpiresAt = s.ActivatedAt.HasValue
                ? s.ExpiresAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
                : AppResources.PendingEntry;

            FinalPrice = s.FinalPrice;

            IsPending = s.ActivatedAt == null;
            IsActive = s.IsActive && s.ActivatedAt != null && s.ExpiresAt > DateTime.UtcNow;
            IsExpired = s.ActivatedAt != null && (!s.IsActive || s.ExpiresAt <= DateTime.UtcNow);

            if (IsActive)
            {
                var remaining = s.ExpiresAt - DateTime.UtcNow;
                UpdateTimeDisplay(remaining);
                StartCountdown(s.ExpiresAt);
            }
            else
            {
                TimeRemaining = IsPending ? AppResources.PendingEntry : AppResources.TimeExpired;
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
    private async Task EndSession()
    {
        if (Session == null) return;

        bool confirm = await Shell.Current.DisplayAlert(
            AppResources.EndSession,
            string.Format(AppResources.EndSessionConfirmMessage, ClientName),
            AppResources.EndButton, AppResources.CancelButton);

        if (!confirm) return;

        IsBusy = true;
        try
        {
            await _sessionService.EndSessionAsync(Session.Id);
            IsActive = false;
            IsExpired = true;
            TimeRemaining = "00:00";
            StopTimer();
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

    partial void OnSelectedExtendTariffChanged(Tariff? value)
    {
        if (value != null)
        {
            var totalMinutes = (int)value.Duration.TotalMinutes;
            CustomExtendHours = (totalMinutes / 60).ToString();
            CustomExtendMinutes = (totalMinutes % 60).ToString();
        }
        RecalculateExtendPrice();
    }

    partial void OnSelectedExtendPromotionChanged(Promotion? value) => RecalculateExtendPrice();
    partial void OnCustomExtendHoursChanged(string value) => RecalculateExtendPrice();
    partial void OnCustomExtendMinutesChanged(string value) => RecalculateExtendPrice();

    private int GetCustomExtendDurationMinutes()
    {
        var hours = int.TryParse(CustomExtendHours, out var h) ? h : 0;
        var minutes = int.TryParse(CustomExtendMinutes, out var m) ? m : 0;
        return Math.Max(1, hours * 60 + minutes);
    }

    private void RecalculateExtendPrice()
    {
        if (SelectedExtendTariff == null)
        {
            ExtendPrice = 0;
            return;
        }

        var customMinutes = GetCustomExtendDurationMinutes();
        var tariffMinutes = SelectedExtendTariff.Duration.TotalMinutes;
        var price = tariffMinutes > 0
            ? SelectedExtendTariff.Price * ((decimal)customMinutes / (decimal)tariffMinutes)
            : SelectedExtendTariff.Price;

        if (SelectedExtendPromotion != null)
        {
            if (SelectedExtendPromotion.DiscountType == DiscountType.Percent)
                price -= price * (SelectedExtendPromotion.DiscountValue / 100);
            else
                price -= SelectedExtendPromotion.DiscountValue;
        }

        ExtendPrice = Math.Round(Math.Max(0, price), 2);
    }

    [RelayCommand]
    private void ToggleExtendPanel()
    {
        IsExtendPanelVisible = !IsExtendPanelVisible;
        if (IsExtendPanelVisible && ExtendTariffs.Count == 0)
        {
            _ = LoadExtendData();
        }
    }

    private async Task LoadExtendData()
    {
        if (Session?.Tariff?.Zone == null) return;
        IsBusy = true;
        try
        {
            var zonesResult = await _zoneService.GetAllWithTariffsAsync();
            var currentZone = zonesResult?.FirstOrDefault(z => z.Id == Session.Tariff.Zone.Id);
            ExtendTariffs = new ObservableCollection<Tariff>(currentZone?.Tariffs ?? []);

            var promosResult = await _promotionService.GetActiveAsync();
            ExtendPromotions = new ObservableCollection<Promotion>(promosResult ?? []);
        }
        catch (Exception)
        {
            // Ignore loading errors
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ConfirmExtension()
    {
        if (Session == null) return;
        if (SelectedExtendTariff == null)
        {
            await Shell.Current.DisplayAlert(AppResources.Error, AppResources.SelectTariffForExtension, "OK");
            return;
        }

        IsBusy = true;
        try
        {
            var customMinutes = GetCustomExtendDurationMinutes();
            var tariffMinutes = (int)SelectedExtendTariff.Duration.TotalMinutes;
            var effectiveMinutes = customMinutes != tariffMinutes ? customMinutes : (int?)null;

            var updated = await _sessionService.ExtendSessionAsync(
                Session.Id, SelectedExtendTariff.Id, SelectedExtendPromotion?.Id, effectiveMinutes);

            if (updated == null)
            {
                await Shell.Current.DisplayAlert(AppResources.Error, AppResources.ExtendSessionError, "OK");
                return;
            }

            await Shell.Current.DisplayAlert(AppResources.Success, AppResources.ExtendSessionSuccess, "OK");
            IsExtendPanelVisible = false;
            await LoadSession();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert(AppResources.Error, string.Format(AppResources.ExtendSessionErrorFormat, ex.Message), "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void StartCountdown(DateTime expiresAt)
    {
        StopTimer();
        _timer = Application.Current?.Dispatcher.CreateTimer();
        if (_timer == null) return;

        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) =>
        {
            var remaining = expiresAt - DateTime.UtcNow;
            if (remaining.TotalSeconds <= 0)
            {
                TimeRemaining = "00:00";
                IsExpired = true;
                StopTimer();
                return;
            }
            UpdateTimeDisplay(remaining);
        };
        _timer.Start();
    }

    private void StopTimer()
    {
        _timer?.Stop();
        _timer = null;
    }

    private void UpdateTimeDisplay(TimeSpan remaining)
    {
        var hours = (int)remaining.TotalHours;
        var mins = remaining.Minutes;
        var secs = remaining.Seconds;
        TimeRemaining = hours > 0
            ? $"{hours}:{mins:D2}:{secs:D2}"
            : $"{mins:D2}:{secs:D2}";
    }
}
