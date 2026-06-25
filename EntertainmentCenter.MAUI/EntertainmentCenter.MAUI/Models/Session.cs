using EntertainmentCenter.Resources.Strings;

namespace EntertainmentCenter.Models;

public class Session
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client? Client { get; set; }
    public int TariffId { get; set; }
    public Tariff? Tariff { get; set; }
    public int? PromotionId { get; set; }
    public Promotion? Promotion { get; set; }
    public decimal FinalPrice { get; set; }
    /// <summary>Время регистрации у стойки</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Время первого сканирования (null = ещё не активирована)</summary>
    public DateTime? ActivatedAt { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }

    public string StatusText => ActivatedAt.HasValue
        ? ((IsActive && ExpiresAt > DateTime.UtcNow) ? AppResources.Active : AppResources.SessionCompleted)
        : AppResources.PendingEntry;

    public string StatusColor => ActivatedAt.HasValue
        ? ((IsActive && ExpiresAt > DateTime.UtcNow) ? "#15803D" : "#6B7280")
        : "#D97706";

    public string StatusBgColor => ActivatedAt.HasValue
        ? ((IsActive && ExpiresAt > DateTime.UtcNow) ? "#DCFCE7" : "#F3F4F6")
        : "#FEF3C7";

    public string DisplayTime => ActivatedAt.HasValue
        ? ActivatedAt.Value.ToLocalTime().ToString("dd.MM HH:mm")
        : CreatedAt.ToLocalTime().ToString("dd.MM HH:mm");
}

