using System.Text.Json.Serialization;

namespace EntertainmentCenter.API.Models;

public class Tariff
{
    public int Id { get; set; }
    public int ZoneId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Zone? Zone { get; set; }
    public string Label { get; set; } = "";
    public TimeSpan Duration { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
}