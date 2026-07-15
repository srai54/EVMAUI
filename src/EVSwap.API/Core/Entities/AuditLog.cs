namespace EVSwap.API.Core.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
