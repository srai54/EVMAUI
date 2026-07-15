namespace EVSwap.API.Core.Entities;

public class FleetAssignment
{
    public int Id { get; set; }
    public int FleetManagerId { get; set; }
    public int VehicleId { get; set; }
    public int DriverId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Active";

    public User FleetManager { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
    public User Driver { get; set; } = null!;
}
