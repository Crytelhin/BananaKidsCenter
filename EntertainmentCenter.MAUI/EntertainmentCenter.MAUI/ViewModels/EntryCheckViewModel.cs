using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EntertainmentCenter.Services;
using EntertainmentCenter.Resources.Strings;

namespace EntertainmentCenter.ViewModels;

public partial class EntryCheckViewModel : BaseConnectionViewModel
{
    private readonly SessionApiService _sessionService;
    private readonly AdminApiService _adminService;
    private readonly IBarcodeScannerService _barcodeScannerService;
    private IDispatcherTimer? _timer;
    private TimeSpan _warningThreshold = TimeSpan.Zero;

    [ObservableProperty]
    private string scannedCode = "";

    [ObservableProperty]
    private bool isAllowed;

    [ObservableProperty]
    private string clientName = "";

    [ObservableProperty]
    private string zoneName = "";

    [ObservableProperty]
    private string timeRemaining = "";

    [ObservableProperty]
    private string statusMessage = "";

    [ObservableProperty]
    private bool isScanning;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private ObservableCollection<SessionDisplay> activeSessions = [];

    [ObservableProperty]
    private bool showDenied;

    [ObservableProperty]
    private bool hasResult;

    public EntryCheckViewModel(
        SessionApiService sessionService, 
        ServerAddressManager addressManager, 
        AdminApiService adminService,
        IBarcodeScannerService barcodeScannerService) : base(addressManager)
    {
        _sessionService = sessionService;
        _adminService = adminService;
        _barcodeScannerService = barcodeScannerService;
    }

    /// <summary>
    /// Called from EntryCheckPage.OnAppearing when returning from scanner with a scanned code.
    /// </summary>
    public async Task ProcessScannedCodeAsync(string code)
    {
        Console.WriteLine($"[EntryCheckVM] ProcessScannedCodeAsync: {code}");
        // Brief delay to let the page fully appear before checking
        await Task.Delay(200);
        await CheckEntry(code);
    }

    [RelayCommand]
    private async Task LoadActiveSessions()
    {
        IsBusy = true;
        try
        {
            // Fetch notification settings to know warning threshold
            var settings = await _adminService.GetNotificationSettingsAsync();
            _warningThreshold = settings?.WarningEnabled == true
                ? TimeSpan.FromMinutes(settings.WarningMinutesBeforeExpiry)
                : TimeSpan.Zero;

            var sessions = await _sessionService.GetAllActiveAsync();
            var now = DateTime.UtcNow;
            var displaySessions = (sessions ?? [])
                .Where(s => s.ActivatedAt != null)
                .Select(s =>
                {
                    var display = new SessionDisplay { Session = s };
                    display.UpdateTimeRemaining();
                    // Mark as warning if within threshold but not yet expired
                    if (_warningThreshold > TimeSpan.Zero)
                    {
                        var remaining = s.ExpiresAt - now;
                        display.IsWarning = remaining <= _warningThreshold && remaining > TimeSpan.Zero;
                    }
                    return display;
                })
                .ToList();

            ActiveSessions = new ObservableCollection<SessionDisplay>(displaySessions);
            StartCountdownTimer();
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
    private async Task OpenScanner()
    {
        IsBusy = true;
        try
        {
            var code = await _barcodeScannerService.ScanAsync();
            if (!string.IsNullOrWhiteSpace(code))
            {
                await CheckEntry(code);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CheckManualEntry()
    {
        await CheckEntry(ScannedCode);
    }

    private async Task CheckEntry(string code)
    {
        Console.WriteLine($"[EntryCheckVM] CheckEntry called with code: {code}");
        if (string.IsNullOrWhiteSpace(code)) return;

        IsBusy = true;
        StopTimer();
        ClearDisplay();

        try
        {
            Console.WriteLine($"[EntryCheckVM] Calling CheckEntryAsync API...");
            var session = await _sessionService.CheckEntryAsync(code);
            Console.WriteLine($"[EntryCheckVM] API response: session={(session != null ? session.Id : "null")}, client={session?.Client?.FullName}");
            HasResult = true;

            if (session == null)
            {
                IsAllowed = false;
                ShowDenied = true;
                StatusMessage = AppResources.CardNotFound;
                ClientName = AppResources.CardNotFound;
                return;
            }

            var remaining = session.ExpiresAt - DateTime.UtcNow;
            if (remaining.TotalSeconds <= 0)
            {
                IsAllowed = false;
                ShowDenied = true;
                StatusMessage = AppResources.SessionExpired;
                ClientName = session.Client?.FullName ?? "";
                ZoneName = session.Tariff?.Zone?.Name ?? "";
                TimeRemaining = AppResources.SessionExpired;
                return;
            }

            IsAllowed = true;
            ShowDenied = false;
            StatusMessage = AppResources.EntryAllowed;
            ClientName = session.Client?.FullName ?? "";
            ZoneName = session.Tariff?.Zone?.Name ?? "";
            UpdateTimeDisplay(remaining);
            StartCountdown(session.ExpiresAt);
        }
        catch (HttpRequestException)
        {
            HasResult = true;
            IsAllowed = false;
            ShowDenied = true;
            StatusMessage = AppResources.NoConnectionToServer;
        }
        catch (Exception)
        {
            HasResult = true;
            IsAllowed = false;
            ShowDenied = true;
            StatusMessage = AppResources.ServerError;
        }
        finally
        {
            IsBusy = false;
            ScannedCode = "";
        }
    }

    private void ClearDisplay()
    {
        IsAllowed = false;
        ShowDenied = false;
        HasResult = false;
        ClientName = "";
        ZoneName = "";
        TimeRemaining = "";
        StatusMessage = "";
    }

    private void StartCountdown(DateTime expiresAt)
    {
        _timer?.Stop();
        _timer = Application.Current?.Dispatcher.CreateTimer();
        if (_timer == null) return;

        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) =>
        {
            var remaining = expiresAt - DateTime.UtcNow;
            if (remaining.TotalSeconds <= 0)
            {
                TimeRemaining = AppResources.SessionExpired;
                IsAllowed = false;
                StatusMessage = AppResources.SessionExpired;
                StopTimer();
                return;
            }
            UpdateTimeDisplay(remaining);
        };
        _timer.Start();
    }

    private void StartCountdownTimer()
    {
        _timer?.Stop();
        _timer = Application.Current?.Dispatcher.CreateTimer();
        if (_timer == null) return;

        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) =>
        {
            var now = DateTime.UtcNow;
            foreach (var session in ActiveSessions)
            {
                session.UpdateTimeRemaining();
                // Update warning flag in real-time
                if (_warningThreshold > TimeSpan.Zero && session.Session?.ActivatedAt != null)
                {
                    var remaining = session.Session.ExpiresAt - now;
                    session.IsWarning = remaining <= _warningThreshold && remaining > TimeSpan.Zero;
                }
            }
        };
        _timer.Start();
    }

    private void StopTimer()
    {
        _timer?.Stop();
    }

    [RelayCommand]
    private async Task NavigateToSettings()
    {
        await Shell.Current.GoToAsync("SettingsPage");
    }

    [RelayCommand]
    private async Task NavigateToClientDetail(SessionDisplay display)
    {
        if (display?.Session?.Id > 0)
            await Shell.Current.GoToAsync($"ClientDetailPage?sessionId={display.Session.Id}");
    }

    private void UpdateTimeDisplay(TimeSpan remaining)
    {
        var hours = (int)remaining.TotalHours;
        var minutes = remaining.Minutes;
        var seconds = remaining.Seconds;
        
        TimeRemaining = hours > 0
            ? $"{hours}{AppResources.HoursUnit} {minutes:D2}{AppResources.MinutesUnit} {seconds:D2}{AppResources.SecondsUnit}"
            : $"{minutes:D2}{AppResources.MinutesUnit} {seconds:D2}{AppResources.SecondsUnit}";
    }
}
