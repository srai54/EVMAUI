using EVSwap.API.Core.Entities;

namespace EVSwap.API.Core.Interfaces.Repositories;

public interface IMaintenanceRepository : IRepository<MaintenanceRequest>
{
    Task<IEnumerable<MaintenanceRequest>> GetPendingAsync();
    Task<IEnumerable<MaintenanceRequest>> GetByBatteryAsync(int batteryId);
}
