using EVSwap.API.Core.DTOs.Battery;

namespace EVSwap.API.Core.Interfaces.Services;

public interface IBatteryService
{
    Task<BatteryDto> GetByIdAsync(int id);
    Task<IEnumerable<BatteryDto>> GetAllAsync();
    Task<IEnumerable<BatteryDto>> GetNearbyAsync(int stationId);
    Task<BatteryDto> UpdateStatusAsync(int id, string status);
    Task<BatteryHealthDto> RecordHealthAsync(int batteryId, BatteryHealthDto healthDto);
}
