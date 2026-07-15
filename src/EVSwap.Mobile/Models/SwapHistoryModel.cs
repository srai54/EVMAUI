namespace EVSwap.Mobile.Models;

public class SwapHistoryModel
{
    public int Id { get; set; }
    public int SwapRequestId { get; set; }
    public int RiderId { get; set; }
    public int StationId { get; set; }
    public string OldBatterySerial { get; set; } = string.Empty;
    public string NewBatterySerial { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
}
