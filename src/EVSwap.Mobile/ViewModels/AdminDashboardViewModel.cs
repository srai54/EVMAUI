using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EVSwap.Mobile.Helpers;
using EVSwap.Mobile.Interfaces;
using EVSwap.Mobile.Models;

namespace EVSwap.Mobile.ViewModels;

public partial class AdminDashboardViewModel : BaseViewModel
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private DashboardModel? _dashboard;

    public AdminDashboardViewModel(
        IApiService apiService,
        INavigationService navigationService,
        IConnectivityService connectivityService)
        : base(navigationService, connectivityService)
    {
        _apiService = apiService;
        Title = "Admin Dashboard";
    }

    [RelayCommand]
    private async Task LoadDashboardAsync()
    {
        IsBusy = true;
        try
        {
            Dashboard = await _apiService.GetAsync<DashboardModel>("/api/admin/dashboard");
        }
        catch
        {
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToUserManagementAsync()
    {
        await NavigationService.NavigateToAsync(Constants.Routes.UserManagement);
    }

    [RelayCommand]
    private async Task NavigateToFleetAsync()
    {
        await NavigationService.NavigateToAsync(Constants.Routes.FleetDashboard);
    }

    [RelayCommand]
    private async Task NavigateToMaintenanceAsync()
    {
        await NavigationService.NavigateToAsync(Constants.Routes.MaintenanceDashboard);
    }
}
