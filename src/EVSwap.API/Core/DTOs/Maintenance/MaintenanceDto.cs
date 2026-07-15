namespace EVSwap.API.Core.DTOs.Maintenance;

public class MaintenanceDto
{
    public int Id { get; set; }
    public int BatteryId { get; set; }
    public string BatterySerial { get; set; } = string.Empty;
    public int? EngineerId { get; set; }
    public string EngineerName { get; set; } = string.Empty;
    public string IssueType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
