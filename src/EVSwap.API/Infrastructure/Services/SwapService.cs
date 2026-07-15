using EVSwap.API.Core.Interfaces.Services;
using EVSwap.API.Core.DTOs.Swap;
using EVSwap.API.Core.Entities;
using EVSwap.API.Core.Interfaces.Repositories;

namespace EVSwap.API.Infrastructure.Services;

public class SwapService : ISwapService
{
    private readonly ISwapRepository _swapRepository;
    private readonly IBatteryRepository _batteryRepository;
    private readonly INotificationRepository _notificationRepository;

    public SwapService(ISwapRepository swapRepository, IBatteryRepository batteryRepository,
        INotificationRepository notificationRepository)
    {
        _swapRepository = swapRepository;
        _batteryRepository = batteryRepository;
        _notificationRepository = notificationRepository;
    }

    public async Task<SwapRequestDto> RequestSwapAsync(int riderId, SwapRequestDto request)
    {
        var swapRequest = new BatterySwapRequest
        {
            RiderId = riderId,
            StationId = request.StationId,
            VehicleId = request.VehicleId,
            OldBatteryId = request.OldBatteryId,
            RequestedBatteryType = request.RequestedBatteryType,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        swapRequest = await _swapRepository.CreateRequestAsync(swapRequest);

        return new SwapRequestDto
        {
            Id = swapRequest.Id,
            RiderId = riderId,
            StationId = swapRequest.StationId,
            VehicleId = swapRequest.VehicleId,
            OldBatteryId = swapRequest.OldBatteryId,
            RequestedBatteryType = swapRequest.RequestedBatteryType,
            Status = swapRequest.Status,
            CreatedAt = swapRequest.CreatedAt
        };
    }

    public async Task<SwapRequestDto> ApproveSwapAsync(int requestId, int newBatteryId)
    {
        var request = await _swapRepository.GetRequestByIdAsync(requestId)
            ?? throw new KeyNotFoundException("Swap request not found");

        var newBattery = await _batteryRepository.GetByIdAsync(newBatteryId)
            ?? throw new KeyNotFoundException("Battery not found");

        request.Status = "Approved";
        newBattery.Status = "InUse";
        await _batteryRepository.UpdateAsync(newBattery);

        if (request.OldBatteryId.HasValue)
        {
            var oldBattery = await _batteryRepository.GetByIdAsync(request.OldBatteryId.Value);
            if (oldBattery != null)
            {
                oldBattery.Status = "Available";
                await _batteryRepository.UpdateAsync(oldBattery);
            }
        }

        await _swapRepository.UpdateRequestAsync(request);

        var history = new BatterySwapHistory
        {
            SwapRequestId = request.Id,
            RiderId = request.RiderId,
            StationId = request.StationId,
            OldBatteryId = request.OldBatteryId ?? 0,
            NewBatteryId = newBatteryId,
            CompletedAt = DateTime.UtcNow
        };

        await _swapRepository.CompleteSwapAsync(history);

        var notification = new Notification
        {
            UserId = request.RiderId,
            Title = "Swap Approved",
            Message = $"Your battery swap request has been approved. New battery serial: {newBattery.SerialNumber}",
            Type = "SwapUpdate",
            CreatedAt = DateTime.UtcNow
        };
        await _notificationRepository.AddAsync(notification);

        return new SwapRequestDto
        {
            Id = request.Id,
            RiderId = request.RiderId,
            StationId = request.StationId,
            VehicleId = request.VehicleId,
            OldBatteryId = request.OldBatteryId,
            RequestedBatteryType = request.RequestedBatteryType,
            Status = request.Status,
            CreatedAt = request.CreatedAt
        };
    }

    public async Task<IEnumerable<SwapHistoryDto>> GetHistoryAsync(int userId)
    {
        var history = await _swapRepository.GetHistoryByUserAsync(userId);
        return history.Select(h => new SwapHistoryDto
        {
            Id = h.Id,
            SwapRequestId = h.SwapRequestId,
            RiderId = h.RiderId,
            RiderName = "",
            StationId = h.StationId,
            StationName = h.Station?.Name ?? "",
            OldBatteryId = h.OldBatteryId,
            OldBatterySerial = h.OldBattery?.SerialNumber ?? "",
            NewBatteryId = h.NewBatteryId,
            NewBatterySerial = h.NewBattery?.SerialNumber ?? "",
            CompletedAt = h.CompletedAt
        });
    }

    public async Task<IEnumerable<SwapRequestDto>> GetPendingRequestsAsync()
    {
        var requests = await _swapRepository.GetPendingRequestsAsync();
        return requests.Select(r => new SwapRequestDto
        {
            Id = r.Id,
            RiderId = r.RiderId,
            RiderName = r.Rider?.Username ?? "",
            StationId = r.StationId,
            StationName = r.Station?.Name ?? "",
            VehicleId = r.VehicleId,
            VehicleRegNumber = r.Vehicle?.RegNumber ?? "",
            OldBatteryId = r.OldBatteryId,
            RequestedBatteryType = r.RequestedBatteryType,
            Status = r.Status,
            CreatedAt = r.CreatedAt
        });
    }
}
