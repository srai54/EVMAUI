using EVSwap.API.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using EVSwap.API.Infrastructure.Data;
using EVSwap.API.Core.Entities;

namespace EVSwap.API.Infrastructure.Repositories;

public class MaintenanceRepository : Repository<MaintenanceRequest>, IMaintenanceRepository
{
    public MaintenanceRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<MaintenanceRequest>> GetPendingAsync()
        => await _dbSet.Include(m => m.Battery)
            .Where(m => m.Status == "Pending")
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<MaintenanceRequest>> GetByBatteryAsync(int batteryId)
        => await _dbSet.Include(m => m.Battery)
            .Where(m => m.BatteryId == batteryId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
}
