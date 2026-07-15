using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EVSwap.Mobile.Interfaces;
using EVSwap.Mobile.Models;

namespace EVSwap.Mobile.ViewModels;

public partial class MaintenanceViewModel : BaseViewModel
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private ObservableCollection<MaintenanceModel> _maintenanceRequests = new();

    [ObservableProperty]
    private ObservableCollection<BatteryModel> _faultBatteries = new();

    public MaintenanceViewModel(
        IApiService apiService,
        INavigationService navigationService,
        IConnectivityService connectivityService)
        : base(navigationService, connectivityService)
    {
        _apiService = apiService;
        Title = "Maintenance";
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsBusy = true;
        try
        {
            var maintenance = await _apiService.GetAsync<List<MaintenanceModel>>("/api/maintenance");
            if (maintenance is not null)
            {
                MaintenanceRequests.Clear();
                foreach (var m in maintenance)
                    MaintenanceRequests.Add(m);
            }

            var batteries = await _apiService.GetAsync<List<BatteryModel>>("/api/battery/fault");
            if (batteries is not null)
            {
                FaultBatteries.Clear();
                foreach (var b in batteries)
                    FaultBatteries.Add(b);
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
}
