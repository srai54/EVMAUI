namespace EVSwap.API.Core.DTOs.Battery;

public class BatteryHealthDto
{
    public int Id { get; set; }
    public int BatteryId { get; set; }
    public DateTime Timestamp { get; set; }
    public double ChargeLevel { get; set; }
    public double Temperature { get; set; }
    public double Voltage { get; set; }
    public int CycleCount { get; set; }
    public string Notes { get; set; } = string.Empty;
}
