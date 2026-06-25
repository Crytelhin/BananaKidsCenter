using CommunityToolkit.Mvvm.ComponentModel;

namespace EntertainmentCenter.ViewModels;

public partial class TariffEdit : ObservableObject
{
    // Original tariff reference for save
    public int Id { get; set; }
    public int ZoneId { get; set; }
    public bool IsActive { get; set; } = true;

    [ObservableProperty]
    private string label = "";

    [ObservableProperty]
    private int durationHours = 1;

    [ObservableProperty]
    private int durationMinutes;

    [ObservableProperty]
    private decimal price;

    public TimeSpan Duration => new TimeSpan(DurationHours, DurationMinutes, 0);

    public static TariffEdit FromTariff(Models.Tariff tariff)
    {
        return new TariffEdit
        {
            Id = tariff.Id,
            ZoneId = tariff.ZoneId,
            IsActive = tariff.IsActive,
            Label = tariff.Label,
            DurationHours = (int)tariff.Duration.TotalHours,
            DurationMinutes = tariff.Duration.Minutes,
            Price = tariff.Price
        };
    }

    public Models.Tariff ToTariff()
    {
        return new Models.Tariff
        {
            Id = Id,
            ZoneId = ZoneId,
            IsActive = IsActive,
            Label = Label,
            Duration = Duration,
            Price = Price
        };
    }
}
