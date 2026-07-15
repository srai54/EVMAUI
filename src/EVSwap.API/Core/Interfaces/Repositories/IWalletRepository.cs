using EVSwap.API.Core.Entities;

namespace EVSwap.API.Core.Interfaces.Repositories;

public interface IWalletRepository : IRepository<Wallet>
{
    Task<Wallet?> GetByUserAsync(int userId);
    Task<Transaction> AddTransactionAsync(Transaction transaction);
    Task<IEnumerable<Transaction>> GetTransactionsAsync(int walletId);
}
