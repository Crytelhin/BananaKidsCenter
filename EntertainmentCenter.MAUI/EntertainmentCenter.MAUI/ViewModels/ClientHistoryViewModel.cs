using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EntertainmentCenter.Models;
using EntertainmentCenter.Services;
using EntertainmentCenter.Resources.Strings;

namespace EntertainmentCenter.ViewModels;

public partial class ClientHistoryViewModel : ObservableObject
{
    private readonly ClientApiService _clientService;
    private readonly SessionApiService _sessionService;

    [ObservableProperty]
    private string searchQuery = "";

    [ObservableProperty]
    private ObservableCollection<Session> sessions = [];

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string selectedFilter = "All";

    [ObservableProperty]
    private bool isToday;

    [ObservableProperty]
    private bool isWeek;

    [ObservableProperty]
    private bool isAll = true;

    public List<string> FilterOptions { get; } = ["Today", "Week", "All"];

    public ClientHistoryViewModel(ClientApiService clientService, SessionApiService sessionService)
    {
        _clientService = clientService;
        _sessionService = sessionService;
    }

    [RelayCommand]
    private async Task NavigateToClientDetail(Session session)
    {
        if (session?.Id > 0)
            await Shell.Current.GoToAsync($"ClientDetailPage?sessionId={session.Id}");
    }

    [RelayCommand]
    private async Task Search()
    {
        IsBusy = true;
        try
        {
            var (from, to) = GetDateRange();

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var clients = await _clientService.SearchAsync(SearchQuery);
                var clientIds = (clients ?? []).Select(c => c.Id).ToHashSet();
                var allSessions = await _sessionService.GetHistoryAsync(from, to);
                Sessions = new ObservableCollection<Session>(
                    (allSessions ?? []).Where(s => clientIds.Contains(s.ClientId)));
            }
            else
            {
                var allSessions = await _sessionService.GetHistoryAsync(from, to);
                Sessions = new ObservableCollection<Session>(allSessions ?? []);
            }

            if (Sessions.Count == 0)
                await Shell.Current.DisplayAlert(AppResources.Error, AppResources.SessionsNotFound, "OK");
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
    private void SetFilter(string filter)
    {
        SelectedFilter = filter;
        IsToday = filter == "Today";
        IsWeek = filter == "Week";
        IsAll = filter == "All";
        SearchCommand.Execute(null);
    }

    private (DateTime from, DateTime to) GetDateRange()
    {
        var now = DateTime.UtcNow;
        return SelectedFilter switch
        {
            "Today" => (now.Date, now.Date.AddDays(1)),
            "Week" => (now.Date.AddDays(-7), now.Date.AddDays(1)),
            _ => (DateTime.MinValue, DateTime.MaxValue)
        };
    }
}
