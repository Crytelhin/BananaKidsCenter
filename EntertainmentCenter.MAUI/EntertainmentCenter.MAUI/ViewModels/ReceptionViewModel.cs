using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EntertainmentCenter.Models;
using EntertainmentCenter.Services;
using EntertainmentCenter.Resources.Strings;

namespace EntertainmentCenter.ViewModels;

public partial class ReceptionViewModel : BaseConnectionViewModel
{
    private readonly ClientApiService _clientService;
    private readonly SessionApiService _sessionService;
    private readonly AdminApiService _adminService;
    private IDispatcherTimer? _timer;
    private TimeSpan _warningThreshold = TimeSpan.Zero;

    [ObservableProperty]
    private string searchQuery = "";

    [ObservableProperty]
    private ObservableCollection<Client> clients = [];

    [ObservableProperty]
    private ObservableCollection<SessionDisplay> activeSessions = [];

    [ObservableProperty]
    private ObservableCollection<object> displayItems = [];

    [ObservableProperty]
    private bool isSearchActive;

    [ObservableProperty]
    private bool isBusy;

    public ReceptionViewModel(ClientApiService clientService, SessionApiService sessionService, AdminApiService adminService, ServerAddressManager addressManager) : base(addressManager)
    {
        _clientService = clientService;
        _sessionService = sessionService;
        _adminService = adminService;
    }

    [RelayCommand]
    private async Task Search()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            Clients = [];
            IsSearchActive = false;
            await LoadActiveSessions();
            return;
        }

        IsBusy = true;
        try
        {
            var results = await _clientService.SearchAsync(SearchQuery);
            Clients = new ObservableCollection<Client>(results ?? []);
            IsSearchActive = Clients.Count > 0;
            if (Clients.Count == 0)
                await Shell.Current.DisplayAlert(AppResources.Error, AppResources.ClientNotFound, "OK");
            else
                DisplayItems = new ObservableCollection<object>(Clients);
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
    private async Task LoadActiveSessions()
    {
        if (IsSearchActive) return;

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
                .Select(s =>
                {
                    var display = new SessionDisplay { Session = s };
                    display.UpdateTimeRemaining();
                    // Mark as warning if within threshold but not yet expired
                    if (_warningThreshold > TimeSpan.Zero && s.ActivatedAt != null)
                    {
                        var remaining = s.ExpiresAt - now;
                        display.IsWarning = remaining <= _warningThreshold && remaining > TimeSpan.Zero;
                    }
                    return display;
                })
                .ToList();

            ActiveSessions = new ObservableCollection<SessionDisplay>(displaySessions);
            DisplayItems = new ObservableCollection<object>(displaySessions.Cast<object>());

            StartTimer();
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
    private async Task NavigateToAddClient()
    {
        await Shell.Current.GoToAsync("AddClientPage");
    }

    [RelayCommand]
    private async Task NavigateToSettings()
    {
        await Shell.Current.GoToAsync("SettingsPage");
    }

    [RelayCommand]
    private async Task NavigateToClientDetail(object item)
    {
        if (item is SessionDisplay display && display?.Session != null)
        {
            await Shell.Current.GoToAsync($"ClientDetailPage?sessionId={display.Session.Id}");
        }
        else if (item is Client client)
        {
            // Look up active session for this client
            var sessions = await _sessionService.GetAllActiveAsync();
            var activeSession = sessions?.FirstOrDefault(s => s.ClientId == client.Id);
            if (activeSession != null)
            {
                await Shell.Current.GoToAsync($"ClientDetailPage?sessionId={activeSession.Id}");
            }
            else
            {
                await Shell.Current.DisplayAlert(AppResources.Error, AppResources.ClientHasNoActiveSession, "OK");
            }
        }
    }

    private void StartTimer()
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
}
