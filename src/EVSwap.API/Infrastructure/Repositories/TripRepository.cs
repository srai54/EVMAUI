using EVSwap.API.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using EVSwap.API.Infrastructure.Data;
using EVSwap.API.Core.Entities;

namespace EVSwap.API.Infrastructure.Repositories;

public class TripRepository : Repository<Trip>, ITripRepository
{
    public TripRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Trip>> GetByUserAsync(int userId)
        => await _dbSet.Include(t => t.Vehicle)
            .Where(t => t.RiderId == userId)
            .OrderByDescending(t => t.StartTime)
            .ToListAsync();

    public async Task<Trip?> GetActiveTripAsync(int userId)
        => await _dbSet.Include(t => t.Vehicle)
            .FirstOrDefaultAsync(t => t.RiderId == userId && t.EndTime == null);
}
