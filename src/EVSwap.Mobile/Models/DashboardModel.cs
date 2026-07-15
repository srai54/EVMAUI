namespace EVSwap.Mobile.Models;

public class DashboardModel
{
    public int TotalUsers { get; set; }
    public int ActiveRiders { get; set; }
    public int BatteriesAvailable { get; set; }
    public int TotalStations { get; set; }
    public int DailySwaps { get; set; }
    public decimal Revenue { get; set; }
}
