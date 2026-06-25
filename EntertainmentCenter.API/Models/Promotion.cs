namespace EntertainmentCenter.API.Models;

public class Promotion
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public DayOfWeek? ApplicableDay { get; set; }
    public bool IsActive { get; set; } = true;
}