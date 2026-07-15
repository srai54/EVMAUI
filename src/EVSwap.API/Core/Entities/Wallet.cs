namespace EVSwap.API.Core.Entities;

public class Wallet
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Balance { get; set; }

    public User User { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
