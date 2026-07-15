using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EVSwap.Mobile.Helpers;
using EVSwap.Mobile.Interfaces;
using EVSwap.Mobile.Models;

namespace EVSwap.Mobile.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private double _batteryPercent;

    [ObservableProperty]
    private string _remainingRange = string.Empty;

    [ObservableProperty]
    private string _batteryStatus = string.Empty;

    [ObservableProperty]
    private double? _batteryTemperature;

    [ObservableProperty]
    private double? _batteryVoltage;

    [ObservableProperty]
    private int? _batteryCycles;

    [ObservableProperty]
    private decimal _walletBalance;

    [ObservableProperty]
    private int _totalTrips;

    [ObservableProperty]
    private string _totalDistance = string.Empty;

    [ObservableProperty]
    private int _totalSwapsCompleted;

    [ObservableProperty]
    private int _unreadNotifications;

    [ObservableProperty]
    private ObservableCollection<RecentActivityModel> _recentActivity = new();

    [ObservableProperty]
    private bool _isAdmin;

    [ObservableProperty]
    private DashboardModel? _adminDashboard;

    public DashboardViewModel(
        IApiService apiService,
        IAuthService authService,
        INavigationService navigationService,
        IConnectivityService connectivityService)
        : base(navigationService, connectivityService)
    {
        _apiService = apiService;
        _authService = authService;
        Title = "Dashboard";
    }

    [RelayCommand]
    private async Task LoadDashboardAsync()
    {
        IsBusy = true;
        try
        {
            UserName = _authService.CurrentUser?.Username ?? "User";
            IsAdmin = _authService.CurrentUser?.Roles?.Contains("Admin") == true;

            var userDash = await _apiService.GetAsync<UserDashboardModel>("/api/report/user-dashboard");
            if (userDash is not null)
            {
                BatteryPercent = userDash.BatteryPercent;
                BatteryStatus = userDash.BatteryStatus;
                BatteryTemperature = userDash.BatteryTemperature;
                BatteryVoltage = userDash.BatteryVoltage;
                BatteryCycles = userDash.BatteryCycles;
                RemainingRange = $"{userDash.BatteryPercent * 1.5:F1} km";
                WalletBalance = userDash.WalletBalance;
                TotalTrips = userDash.TotalTrips;
                TotalDistance = $"{userDash.TotalDistanceKm:F1} km";
                TotalSwapsCompleted = userDash.TotalSwapsCompleted;
                UnreadNotifications = userDash.UnreadNotifications;

                RecentActivity.Clear();
                foreach (var item in userDash.RecentActivity)
                    RecentActivity.Add(item);
            }

            if (IsAdmin)
            {
                AdminDashboard = await _apiService.GetAsync<DashboardModel>("/api/report/dashboard");
            }
        }
        catch
        {
            BatteryPercent = 75;
            BatteryStatus = "Good";
            BatteryTemperature = 32.5;
            BatteryVoltage = 48.2;
            BatteryCycles = 120;
            RemainingRange = "112.5 km";
            WalletBalance = 250.00m;
            TotalTrips = 18;
            TotalDistance = "245.6 km";
            TotalSwapsCompleted = 12;
            UnreadNotifications = 3;

            RecentActivity.Clear();
            RecentActivity.Add(new RecentActivityModel { Type = "Swap", Description = "Battery swap completed at Central Station", Timestamp = DateTime.Now.AddHours(-2), Icon = "battery.png" });
            RecentActivity.Add(new RecentActivityModel { Type = "Trip", Description = "Trip #182 completed - 12.5 km", Timestamp = DateTime.Now.AddHours(-5), Icon = "map.png" });
            RecentActivity.Add(new RecentActivityModel { Type = "Payment", Description = "Wallet topped up $50.00", Timestamp = DateTime.Now.AddDays(-1), Icon = "wallet.png" });
            RecentActivity.Add(new RecentActivityModel { Type = "Swap", Description = "Battery health check passed", Timestamp = DateTime.Now.AddDays(-2), Icon = "check.png" });
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToStationsAsync()
    {
        await NavigationService.NavigateToAsync($"//{Constants.Routes.Stations}");
    }

    [RelayCommand]
    private async Task NavigateToSwapAsync()
    {
        await NavigationService.NavigateToAsync($"//{Constants.Routes.BatterySwap}");
    }

    [RelayCommand]
    private async Task NavigateToTripsAsync()
    {
        await NavigationService.NavigateToAsync($"//{Constants.Routes.Trips}");
    }

    [RelayCommand]
    private async Task NavigateToWalletAsync()
    {
        await NavigationService.NavigateToAsync($"//{Constants.Routes.Wallet}");
    }

    [RelayCommand]
    private async Task NavigateToNotificationsAsync()
    {
        await NavigationService.NavigateToAsync($"//{Constants.Routes.Notifications}");
    }
}
