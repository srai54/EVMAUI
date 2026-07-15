using EVSwap.API.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using EVSwap.API.Infrastructure.Data;
using EVSwap.API.Core.Entities;

namespace EVSwap.API.Infrastructure.Repositories;

public class BatteryRepository : Repository<Battery>, IBatteryRepository
{
    public BatteryRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Battery>> GetByStationAsync(int stationId)
        => await _dbSet.Where(b => b.Status != "Disposed").ToListAsync();

    public async Task<IEnumerable<Battery>> GetAvailableAsync()
        => await _dbSet.Where(b => b.Status == "Available").ToListAsync();
}
