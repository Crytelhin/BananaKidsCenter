namespace EntertainmentCenter.API.Models;

public class Session
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public int TariffId { get; set; }
    public Tariff Tariff { get; set; } = null!;
    public int? PromotionId { get; set; }
    public Promotion? Promotion { get; set; }
    public decimal FinalPrice { get; set; }
    /// <summary>Время регистрации у стойки (сессия ещё не активна)</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Время первого сканирования карточки на контроле (null = ещё не вошёл)</summary>
    public DateTime? ActivatedAt { get; set; }
    /// <summary>Сохранённая длительность в минутах — нужна для расчёта ExpiresAt при активации</summary>
    public int DurationMinutes { get; set; }
    public DateTime ExpiresAt { get; set; }
    /// <summary>true = сессия активирована (клиент прошёл контроль) и не истекла</summary>
    public bool IsActive { get; set; } = false;
}