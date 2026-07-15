using EVSwap.API.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using EVSwap.API.Infrastructure.Data;
using EVSwap.API.Core.Entities;

namespace EVSwap.API.Infrastructure.Repositories;

public class SwapRepository : ISwapRepository
{
    private readonly AppDbContext _context;

    public SwapRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<BatterySwapRequest> CreateRequestAsync(BatterySwapRequest request)
    {
        _context.BatterySwapRequests.Add(request);
        await _context.SaveChangesAsync();
        return request;
    }

    public async Task<BatterySwapRequest?> GetRequestByIdAsync(int id)
        => await _context.BatterySwapRequests
            .Include(r => r.Rider)
            .Include(r => r.Station)
            .Include(r => r.Vehicle)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<IEnumerable<BatterySwapRequest>> GetPendingRequestsAsync()
        => await _context.BatterySwapRequests
            .Include(r => r.Rider)
            .Include(r => r.Station)
            .Include(r => r.Vehicle)
            .Where(r => r.Status == "Pending")
            .ToListAsync();

    public async Task<IEnumerable<BatterySwapHistory>> GetHistoryByUserAsync(int userId)
        => await _context.BatterySwapHistories
            .Include(h => h.Station)
            .Include(h => h.OldBattery)
            .Include(h => h.NewBattery)
            .Where(h => h.RiderId == userId)
            .OrderByDescending(h => h.CompletedAt)
            .ToListAsync();

    public async Task<BatterySwapRequest> UpdateRequestAsync(BatterySwapRequest request)
    {
        _context.BatterySwapRequests.Update(request);
        await _context.SaveChangesAsync();
        return request;
    }

    public async Task<BatterySwapHistory> CompleteSwapAsync(BatterySwapHistory history)
    {
        _context.BatterySwapHistories.Add(history);
        await _context.SaveChangesAsync();
        return history;
    }
}
