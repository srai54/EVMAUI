using EVSwap.API.Core.Entities;

namespace EVSwap.API.Core.Interfaces.Repositories;

public interface IStationRepository : IRepository<Station>
{
    Task<IEnumerable<Station>> GetNearbyAsync(double lat, double lng, double radiusKm);
}
