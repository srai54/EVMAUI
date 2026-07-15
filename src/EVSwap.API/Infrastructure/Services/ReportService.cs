using EVSwap.API.Core.DTOs.Report;
using EVSwap.API.Core.Entities;
using EVSwap.API.Core.Interfaces.Services;
using EVSwap.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var weekStart = now.AddDays(-(int)now.DayOfWeek).Date;

        var totalUsers = await _context.Users.CountAsync();
        var activeRiders = await _context.Users.CountAsync(u => u.IsActive);
        var newUsersToday = await _context.Users.CountAsync(u => u.CreatedAt >= todayStart);
        var newUsersThisWeek = await _context.Users.CountAsync(u => u.CreatedAt >= weekStart);

        var totalStations = await _context.Stations.CountAsync();
        var activeStations = await _context.Stations.CountAsync(s => s.Status == "Active");
        var inactiveStations = await _context.Stations.CountAsync(s => s.Status != "Active");

        var totalBatteries = await _context.Batteries.CountAsync();
        var batteriesAvailable = await _context.Batteries.CountAsync(b => b.Status == "Available");
        var batteriesInUse = await _context.Batteries.CountAsync(b => b.Status == "InUse");
        var batteriesMaintenance = await _context.Batteries.CountAsync(b => b.Status == "Maintenance");
        var batteriesDisposed = await _context.Batteries.CountAsync(b => b.Status == "Disposed");

        var dailySwaps = await _context.BatterySwapHistories.CountAsync(h => h.CompletedAt >= todayStart);
        var weeklySwaps = await _context.BatterySwapHistories.CountAsync(h => h.CompletedAt >= weekStart);
        var totalSwaps = await _context.BatterySwapHistories.CountAsync();
        var pendingSwaps = await _context.BatterySwapRequests.CountAsync(r => r.Status == "Pending");

        var totalRevenue = await _context.Transactions
            .Where(t => t.Type == "Credit").SumAsync(t => (decimal?)t.Amount) ?? 0;
        var todayRevenue = await _context.Transactions
            .Where(t => t.Type == "Credit" && t.Timestamp >= todayStart).SumAsync(t => (decimal?)t.Amount) ?? 0;
        var weeklyRevenue = await _context.Transactions
            .Where(t => t.Type == "Credit" && t.Timestamp >= weekStart).SumAsync(t => (decimal?)t.Amount) ?? 0;

        var activeTrips = await _context.Trips.CountAsync(t => t.EndTime == null);
        var todayTrips = await _context.Trips.CountAsync(t => t.StartTime >= todayStart);
        var totalTrips = await _context.Trips.CountAsync();
        var totalDistance = await _context.Trips.SumAsync(t => (double?)t.DistanceKm) ?? 0;

        var pendingMaintenance = await _context.MaintenanceRequests.CountAsync(m => m.Status == "Pending" || m.Status == "InProgress");
        var totalMaintenance = await _context.MaintenanceRequests.CountAsync();

        var totalFleet = await _context.FleetAssignments.CountAsync();
        var activeFleet = await _context.FleetAssignments.CountAsync(f => f.Status == "Active");

        var openTickets = await _context.SupportTickets.CountAsync(t => t.Status == "Open" || t.Status == "InProgress");

        return new DashboardDto
        {
            TotalUsers = totalUsers,
            ActiveRiders = activeRiders,
            NewUsersToday = newUsersToday,
            NewUsersThisWeek = newUsersThisWeek,
            TotalStations = totalStations,
            ActiveStations = activeStations,
            InactiveStations = inactiveStations,
            TotalBatteries = totalBatteries,
            BatteriesAvailable = batteriesAvailable,
            BatteriesInUse = batteriesInUse,
            BatteriesMaintenance = batteriesMaintenance,
            BatteriesDisposed = batteriesDisposed,
            DailySwaps = dailySwaps,
            WeeklySwaps = weeklySwaps,
            TotalSwaps = totalSwaps,
            PendingSwaps = pendingSwaps,
            TotalRevenue = totalRevenue,
            TodayRevenue = todayRevenue,
            WeeklyRevenue = weeklyRevenue,
            ActiveTrips = activeTrips,
            TodayTrips = todayTrips,
            TotalTrips = totalTrips,
            TotalDistanceKm = totalDistance,
            PendingMaintenance = pendingMaintenance,
            TotalMaintenance = totalMaintenance,
            TotalFleetVehicles = totalFleet,
            ActiveFleetVehicles = activeFleet,
            OpenTickets = openTickets
        };
    }

    public async Task<UserDashboardDto> GetUserDashboardAsync(int userId)
    {
        var battery = await _context.Batteries.FirstOrDefaultAsync(b => b.Id == userId);
        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        var totalTrips = await _context.Trips.CountAsync(t => t.RiderId == userId);
        var totalDistance = await _context.Trips
            .Where(t => t.RiderId == userId).SumAsync(t => (double?)t.DistanceKm) ?? 0;
        var totalSwaps = await _context.BatterySwapHistories.CountAsync(h => h.RiderId == userId);
        var unreadNotifications = await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);

        var recentTrips = await _context.Trips
            .Where(t => t.RiderId == userId && t.EndTime != null)
            .OrderByDescending(t => t.EndTime)
            .Take(3)
            .Select(t => new RecentActivityDto
            {
                Type = "Trip",
                Description = $"Trip completed — {t.DistanceKm:F1} km",
                Timestamp = t.EndTime ?? t.StartTime,
                Icon = "\U0001f697"
            })
            .ToListAsync();

        var recentSwaps = await _context.BatterySwapHistories
            .Where(h => h.RiderId == userId)
            .OrderByDescending(h => h.CompletedAt)
            .Take(3)
            .Select(h => new RecentActivityDto
            {
                Type = "Swap",
                Description = $"Battery swapped at station {h.StationId}",
                Timestamp = h.CompletedAt,
                Icon = "\U0001f50b"
            })
            .ToListAsync();

        var recentNotifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(3)
            .Select(n => new RecentActivityDto
            {
                Type = "Notification",
                Description = n.Title,
                Timestamp = n.CreatedAt,
                Icon = "\U0001f514"
            })
            .ToListAsync();

        var activity = recentTrips
            .Concat(recentSwaps)
            .Concat(recentNotifications)
            .OrderByDescending(a => a.Timestamp)
            .Take(5)
            .ToList();

        return new UserDashboardDto
        {
            BatteryPercent = battery?.ChargeLevel ?? 0,
            BatteryStatus = battery?.Status ?? "Unknown",
            BatteryTemperature = battery?.Temperature,
            BatteryVoltage = battery?.Voltage,
            BatteryCycles = battery?.ChargeCycles,
            WalletBalance = wallet?.Balance ?? 0,
            TotalTrips = totalTrips,
            TotalDistanceKm = totalDistance,
            TotalSwapsCompleted = totalSwaps,
            UnreadNotifications = unreadNotifications,
            RecentActivity = activity
        };
    }
}
