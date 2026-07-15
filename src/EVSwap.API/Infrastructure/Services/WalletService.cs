using EVSwap.API.Core.Entities;
using EVSwap.API.Core.Interfaces.Services;
using EVSwap.API.Core.DTOs.Wallet;
using EVSwap.API.Core.Interfaces.Repositories;

namespace EVSwap.API.Infrastructure.Services;

public class WalletService : IWalletService
{
    private readonly IWalletRepository _walletRepository;

    public WalletService(IWalletRepository walletRepository)
    {
        _walletRepository = walletRepository;
    }

    public async Task<WalletDto> GetBalanceAsync(int userId)
    {
        var wallet = await _walletRepository.GetByUserAsync(userId)
            ?? throw new KeyNotFoundException("Wallet not found");
        return new WalletDto { Id = wallet.Id, UserId = wallet.UserId, Balance = wallet.Balance };
    }

    public async Task<WalletDto> AddMoneyAsync(int userId, AddMoneyRequest request)
    {
        var wallet = await _walletRepository.GetByUserAsync(userId)
            ?? throw new KeyNotFoundException("Wallet not found");

        wallet.Balance += request.Amount;
        await _walletRepository.UpdateAsync(wallet);

        var transaction = new Transaction
        {
            WalletId = wallet.Id,
            Amount = request.Amount,
            Type = "Credit",
            Reference = $"Added {request.Amount:C} to wallet",
            Timestamp = DateTime.UtcNow
        };
        await _walletRepository.AddTransactionAsync(transaction);

        return new WalletDto { Id = wallet.Id, UserId = wallet.UserId, Balance = wallet.Balance };
    }

    public async Task<IEnumerable<TransactionDto>> GetTransactionsAsync(int userId)
    {
        var wallet = await _walletRepository.GetByUserAsync(userId)
            ?? throw new KeyNotFoundException("Wallet not found");

        var transactions = await _walletRepository.GetTransactionsAsync(wallet.Id);
        return transactions.Select(t => new TransactionDto
        {
            Id = t.Id,
            WalletId = t.WalletId,
            Amount = t.Amount,
            Type = t.Type,
            Reference = t.Reference,
            Timestamp = t.Timestamp
        });
    }
}
