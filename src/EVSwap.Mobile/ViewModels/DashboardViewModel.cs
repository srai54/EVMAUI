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
    private double _batteryPercent;

    [ObservableProperty]
    private string _remainingRange = string.Empty;

    [ObservableProperty]
    private string _vehicleStatus = string.Empty;

    [ObservableProperty]
    private bool _isAdmin;

    [ObservableProperty]
    private DashboardModel? _adminDashboard;

    [ObservableProperty]
    private string _userName = string.Empty;

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

            var battery = await _apiService.GetAsync<BatteryModel>("/api/battery/my");
            if (battery is not null)
            {
                BatteryPercent = battery.ChargeLevel;
                RemainingRange = $"{battery.ChargeLevel * 1.5:F1} km";
                VehicleStatus = battery.Status;
            }

            if (IsAdmin)
            {
                AdminDashboard = await _apiService.GetAsync<DashboardModel>("/api/admin/dashboard");
            }
        }
        catch
        {
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
}
