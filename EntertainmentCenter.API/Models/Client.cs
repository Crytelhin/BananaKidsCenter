namespace EntertainmentCenter.API.Models;

public class Client
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string CardCode { get; set; } = "";
    public bool IsVip { get; set; }
    public bool AcceptsMarketing { get; set; }
    public DateTime RegisteredAt { get; set; }
}