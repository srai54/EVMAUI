namespace EVSwap.API.Core.DTOs.Wallet;

public class TransactionDto
{
    public int Id { get; set; }
    public int WalletId { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
