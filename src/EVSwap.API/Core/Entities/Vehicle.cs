namespace EVSwap.API.Core.Entities;

public class Vehicle
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string RegNumber { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public int? BatteryId { get; set; }

    public User User { get; set; } = null!;
    public Battery? Battery { get; set; }
}
