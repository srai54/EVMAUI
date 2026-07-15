using EVSwap.API.Core.DTOs.Swap;

namespace EVSwap.API.Core.Interfaces.Services;

public interface ISwapService
{
    Task<SwapRequestDto> RequestSwapAsync(int riderId, SwapRequestDto request);
    Task<SwapRequestDto> ApproveSwapAsync(int requestId, int newBatteryId);
    Task<IEnumerable<SwapHistoryDto>> GetHistoryAsync(int userId);
    Task<IEnumerable<SwapRequestDto>> GetPendingRequestsAsync();
}
