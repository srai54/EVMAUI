using EVSwap.API.Core.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using EVSwap.API.Infrastructure.Data;
using EVSwap.API.Core.DTOs.Report;

namespace EVSwap.API.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _context;

    public ReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        var totalUsers = await _context.Users.CountAsync();
        var activeRiders = await _context.Users.CountAsync(u => u.IsActive);
        var batteriesAvailable = await _context.Batteries.CountAsync(b => b.Status == "Available");
        var totalStations = await _context.Stations.CountAsync(s => s.Status == "Active");
        var dailySwaps = await _context.BatterySwapHistories
            .CountAsync(h => h.CompletedAt.Date == DateTime.UtcNow.Date);
        var revenue = await _context.Wallets.SumAsync(w => w.Balance);

        return new DashboardDto
        {
            TotalUsers = totalUsers,
            ActiveRiders = activeRiders,
            BatteriesAvailable = batteriesAvailable,
            TotalStations = totalStations,
            DailySwaps = dailySwaps,
            Revenue = revenue
        };
    }
}
