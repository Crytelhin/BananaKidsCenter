namespace EntertainmentCenter.API.Models;

public class AppConfig
{
    public int Id { get; set; } = 1;
    public string AdminPin { get; set; } = "1234";
    public bool WarningEnabled { get; set; } = true;
    public int WarningMinutesBeforeExpiry { get; set; } = 5;
}