using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EVSwap.Mobile.Helpers;
using EVSwap.Mobile.Interfaces;
using EVSwap.Mobile.Models;

namespace EVSwap.Mobile.ViewModels;

public partial class BatterySwapViewModel : BaseViewModel
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private ObservableCollection<SwapRequestModel> _activeRequests = new();

    [ObservableProperty]
    private ObservableCollection<SwapHistoryModel> _swapHistory = new();

    [ObservableProperty]
    private bool _showActiveRequests = true;

    [ObservableProperty]
    private string _selectedBatteryType = "Standard";

    public List<string> BatteryTypes { get; } = new() { "Standard", "Premium", "LongRange" };

    public BatterySwapViewModel(
        IApiService apiService,
        INavigationService navigationService,
        IConnectivityService connectivityService)
        : base(navigationService, connectivityService)
    {
        _apiService = apiService;
        Title = "Battery Swaps";
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsBusy = true;
        try
        {
            var requests = await _apiService.GetAsync<List<SwapRequestModel>>("/api/swap/requests");
            if (requests is not null)
            {
                ActiveRequests.Clear();
                foreach (var r in requests.Where(r => r.Status is "Pending" or "InProgress"))
                    ActiveRequests.Add(r);
            }

            var history = await _apiService.GetAsync<List<SwapHistoryModel>>("/api/swap/history");
            if (history is not null)
            {
                SwapHistory.Clear();
                foreach (var h in history)
                    SwapHistory.Add(h);
            }
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
    private async Task RequestSwapAsync()
    {
        await NavigationService.NavigateToAsync(Constants.Routes.SwapRequest);
    }

    [RelayCommand]
    private async Task ScanQrAsync()
    {
        await NavigationService.NavigateToAsync(Constants.Routes.QRScan);
    }

    [RelayCommand]
    private void ToggleView()
    {
        ShowActiveRequests = !ShowActiveRequests;
    }
}
