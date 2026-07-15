namespace EVSwap.API.Core.Entities;

public class Transaction
{
    public int Id { get; set; }
    public int WalletId { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public Wallet Wallet { get; set; } = null!;
}
