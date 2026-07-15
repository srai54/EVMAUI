namespace EVSwap.Mobile.Models;

public class SwapRequestModel
{
    public int Id { get; set; }
    public int RiderId { get; set; }
    public int StationId { get; set; }
    public int VehicleId { get; set; }
    public int OldBatteryId { get; set; }
    public string RequestedBatteryType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string StationName { get; set; } = string.Empty;
    public string RiderName { get; set; } = string.Empty;
}
