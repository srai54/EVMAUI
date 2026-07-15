namespace EVSwap.API.Core.DTOs.Fleet;

public class FleetDto
{
    public int Id { get; set; }
    public int FleetManagerId { get; set; }
    public string FleetManagerName { get; set; } = string.Empty;
    public int VehicleId { get; set; }
    public string VehicleRegNumber { get; set; } = string.Empty;
    public int DriverId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
