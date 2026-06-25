namespace EntertainmentCenter.Models;

public class Zone
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public List<Tariff> Tariffs { get; set; } = [];
}
