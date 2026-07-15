namespace EVSwap.API.Core.Entities;

public class MaintenanceRequest
{
    public int Id { get; set; }
    public int BatteryId { get; set; }
    public int? EngineerId { get; set; }
    public string IssueType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    public Battery Battery { get; set; } = null!;
    public User? Engineer { get; set; }
}
