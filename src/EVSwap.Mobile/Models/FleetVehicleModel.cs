namespace EVSwap.Mobile.Models;

public class FleetVehicleModel
{
    public int Id { get; set; }
    public string VehicleRegNumber { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double BatteryLevel { get; set; }
}
