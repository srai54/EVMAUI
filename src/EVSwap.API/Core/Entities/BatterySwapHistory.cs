namespace EVSwap.API.Core.Entities;

public class BatterySwapHistory
{
    public int Id { get; set; }
    public int SwapRequestId { get; set; }
    public int RiderId { get; set; }
    public int StationId { get; set; }
    public int OldBatteryId { get; set; }
    public int NewBatteryId { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    public BatterySwapRequest SwapRequest { get; set; } = null!;
    public User Rider { get; set; } = null!;
    public Station Station { get; set; } = null!;
    public Battery OldBattery { get; set; } = null!;
    public Battery NewBattery { get; set; } = null!;
}
