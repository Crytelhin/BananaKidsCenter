namespace EntertainmentCenter.Models;

public class Staff
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string WorkStation { get; set; } = "";
    public bool IsAdmin { get; set; }
}
