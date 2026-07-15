using EVSwap.API.Core.Entities;

namespace EVSwap.API.Core.Interfaces.Repositories;

public interface IBatteryRepository : IRepository<Battery>
{
    Task<IEnumerable<Battery>> GetByStationAsync(int stationId);
    Task<IEnumerable<Battery>> GetAvailableAsync();
}
