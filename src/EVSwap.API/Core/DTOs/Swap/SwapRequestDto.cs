namespace EVSwap.API.Core.DTOs.Swap;

public class SwapRequestDto
{
    public int Id { get; set; }
    public int RiderId { get; set; }
    public string RiderName { get; set; } = string.Empty;
    public int StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public int VehicleId { get; set; }
    public string VehicleRegNumber { get; set; } = string.Empty;
    public int? OldBatteryId { get; set; }
    public string RequestedBatteryType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
