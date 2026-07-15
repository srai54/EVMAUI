namespace EVSwap.API.Core.Entities;

public class BatteryHealth
{
    public int Id { get; set; }
    public int BatteryId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double ChargeLevel { get; set; }
    public double Temperature { get; set; }
    public double Voltage { get; set; }
    public int CycleCount { get; set; }
    public string Notes { get; set; } = string.Empty;

    public Battery Battery { get; set; } = null!;
}
