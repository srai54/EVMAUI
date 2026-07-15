using EVSwap.API.Core.DTOs.Wallet;

namespace EVSwap.API.Core.Interfaces.Services;

public interface IWalletService
{
    Task<WalletDto> GetBalanceAsync(int userId);
    Task<WalletDto> AddMoneyAsync(int userId, AddMoneyRequest request);
    Task<IEnumerable<TransactionDto>> GetTransactionsAsync(int userId);
}
