using EVSwap.API.Core.DTOs.Maintenance;

namespace EVSwap.API.Core.Interfaces.Services;

public interface IMaintenanceService
{
    Task<IEnumerable<MaintenanceDto>> GetRequestsAsync();
    Task<MaintenanceDto> CreateRequestAsync(MaintenanceDto dto);
    Task<MaintenanceDto> ResolveRequestAsync(int id);
    Task<IEnumerable<MaintenanceDto>> GetDiagnosticsAsync(int batteryId);
}
