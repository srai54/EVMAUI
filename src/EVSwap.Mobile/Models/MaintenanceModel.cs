namespace EVSwap.Mobile.Models;

public class MaintenanceModel
{
    public int Id { get; set; }
    public int BatteryId { get; set; }
    public string IssueType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
