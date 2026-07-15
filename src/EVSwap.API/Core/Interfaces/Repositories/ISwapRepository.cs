using EVSwap.API.Core.Entities;

namespace EVSwap.API.Core.Interfaces.Repositories;

public interface ISwapRepository
{
    Task<BatterySwapRequest> CreateRequestAsync(BatterySwapRequest request);
    Task<BatterySwapRequest?> GetRequestByIdAsync(int id);
    Task<IEnumerable<BatterySwapRequest>> GetPendingRequestsAsync();
    Task<IEnumerable<BatterySwapHistory>> GetHistoryByUserAsync(int userId);
    Task<BatterySwapRequest> UpdateRequestAsync(BatterySwapRequest request);
    Task<BatterySwapHistory> CompleteSwapAsync(BatterySwapHistory history);
}
