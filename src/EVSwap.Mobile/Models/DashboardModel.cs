namespace EVSwap.Mobile.Models;

public class DashboardModel
{
    public int TotalUsers { get; set; }
    public int ActiveRiders { get; set; }
    public int NewUsersToday { get; set; }
    public int NewUsersThisWeek { get; set; }
    public int TotalStations { get; set; }
    public int ActiveStations { get; set; }
    public int InactiveStations { get; set; }
    public int TotalBatteries { get; set; }
    public int BatteriesAvailable { get; set; }
    public int BatteriesInUse { get; set; }
    public int BatteriesMaintenance { get; set; }
    public int BatteriesDisposed { get; set; }
    public int DailySwaps { get; set; }
    public int WeeklySwaps { get; set; }
    public int TotalSwaps { get; set; }
    public int PendingSwaps { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TodayRevenue { get; set; }
    public decimal WeeklyRevenue { get; set; }
    public int ActiveTrips { get; set; }
    public int TodayTrips { get; set; }
    public int TotalTrips { get; set; }
    public double TotalDistanceKm { get; set; }
    public int PendingMaintenance { get; set; }
    public int TotalMaintenance { get; set; }
    public int TotalFleetVehicles { get; set; }
    public int ActiveFleetVehicles { get; set; }
    public int OpenTickets { get; set; }
}

public class UserDashboardModel
{
    public double BatteryPercent { get; set; }
    public string BatteryStatus { get; set; } = string.Empty;
    public double? BatteryTemperature { get; set; }
    public double? BatteryVoltage { get; set; }
    public int? BatteryCycles { get; set; }
    public decimal WalletBalance { get; set; }
    public int TotalTrips { get; set; }
    public double TotalDistanceKm { get; set; }
    public int TotalSwapsCompleted { get; set; }
    public int UnreadNotifications { get; set; }
    public List<RecentActivityModel> RecentActivity { get; set; } = new();
}

public class RecentActivityModel
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Icon { get; set; } = string.Empty;
}
