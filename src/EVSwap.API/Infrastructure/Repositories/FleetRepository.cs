using EVSwap.API.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using EVSwap.API.Infrastructure.Data;
using EVSwap.API.Core.Entities;

namespace EVSwap.API.Infrastructure.Repositories;

public class FleetRepository : Repository<FleetAssignment>, IFleetRepository
{
    public FleetRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<FleetAssignment>> GetByManagerAsync(int managerId)
        => await _dbSet
            .Include(f => f.Vehicle)
            .Include(f => f.Driver)
            .Where(f => f.FleetManagerId == managerId)
            .OrderByDescending(f => f.AssignedAt)
            .ToListAsync();
}
