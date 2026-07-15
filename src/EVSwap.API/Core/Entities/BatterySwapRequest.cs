namespace EVSwap.API.Core.Entities;

public class BatterySwapRequest
{
    public int Id { get; set; }
    public int RiderId { get; set; }
    public int StationId { get; set; }
    public int VehicleId { get; set; }
    public int? OldBatteryId { get; set; }
    public string RequestedBatteryType { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User Rider { get; set; } = null!;
    public Station Station { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
}
