using EVSwap.API.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using EVSwap.API.Infrastructure.Data;
using EVSwap.API.Core.Entities;

namespace EVSwap.API.Infrastructure.Repositories;

public class WalletRepository : Repository<Wallet>, IWalletRepository
{
    public WalletRepository(AppDbContext context) : base(context) { }

    public async Task<Wallet?> GetByUserAsync(int userId)
        => await _dbSet.FirstOrDefaultAsync(w => w.UserId == userId);

    public async Task<Transaction> AddTransactionAsync(Transaction transaction)
    {
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsAsync(int walletId)
        => await _context.Transactions
            .Where(t => t.WalletId == walletId)
            .OrderByDescending(t => t.Timestamp)
            .ToListAsync();
}
