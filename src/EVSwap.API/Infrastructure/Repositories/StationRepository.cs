using EVSwap.API.Core.Interfaces.Repositories;
using EVSwap.API.Infrastructure.Data;
using EVSwap.API.Core.Entities;
using EVSwap.API.Infrastructure.Utilities;

namespace EVSwap.API.Infrastructure.Repositories;

public class StationRepository : Repository<Station>, IStationRepository
{
    public StationRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Station>> GetNearbyAsync(double lat, double lng, double radiusKm)
    {
        var allStations = await GetAllAsync();
        return allStations
            .Where(s => DistanceHelper.CalculateDistance(lat, lng, s.Latitude, s.Longitude) <= radiusKm)
            .ToList();
    }
}
