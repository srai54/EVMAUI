namespace EVSwap.API.Core.Entities;

public class SupportTicket
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    public User User { get; set; } = null!;
}
