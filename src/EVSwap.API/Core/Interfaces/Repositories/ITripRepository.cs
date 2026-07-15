using EVSwap.API.Core.Entities;

namespace EVSwap.API.Core.Interfaces.Repositories;

public interface ITripRepository : IRepository<Trip>
{
    Task<IEnumerable<Trip>> GetByUserAsync(int userId);
    Task<Trip?> GetActiveTripAsync(int userId);
}
