namespace EVSwap.API.Core.DTOs.Swap;

public class SwapHistoryDto
{
    public int Id { get; set; }
    public int SwapRequestId { get; set; }
    public int RiderId { get; set; }
    public string RiderName { get; set; } = string.Empty;
    public int StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public int OldBatteryId { get; set; }
    public string OldBatterySerial { get; set; } = string.Empty;
    public int NewBatteryId { get; set; }
    public string NewBatterySerial { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
}
