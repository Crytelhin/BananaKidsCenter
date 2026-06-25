using CommunityToolkit.Mvvm.ComponentModel;
using EntertainmentCenter.Models;
using EntertainmentCenter.Resources.Strings;

namespace EntertainmentCenter.ViewModels;

public partial class SessionDisplay : ObservableObject
{
    public Session Session { get; set; } = null!;

    public string ClientName => Session.Client?.FullName ?? "";
    public string ZoneName => Session.Tariff?.Zone?.Name ?? "";
    public string TariffLabel => Session.Tariff?.Label ?? "";
    public string Phone => Session.Client?.Phone ?? "";
    public decimal FinalPrice => Session.FinalPrice;

    [ObservableProperty]
    private string timeRemaining = "";

    [ObservableProperty]
    private bool isWarning;

    public void UpdateTimeRemaining()
    {
        if (Session.ActivatedAt == null)
        {
            TimeRemaining = AppResources.SessionPending;
            return;
        }
        var remaining = Session.ExpiresAt - DateTime.UtcNow;
        if (remaining.TotalSeconds <= 0)
        {
            TimeRemaining = AppResources.SessionExpired;
        }
        else
        {
            var hours = (int)remaining.TotalHours;
            var minutes = remaining.Minutes;
            var seconds = remaining.Seconds;
            
            TimeRemaining = hours > 0
                ? $"{hours}{AppResources.HoursUnit} {minutes:D2}{AppResources.MinutesUnit} {seconds:D2}{AppResources.SecondsUnit}"
                : $"{minutes:D2}{AppResources.MinutesUnit} {seconds:D2}{AppResources.SecondsUnit}";
        }
    }
}
