using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EVSwap.Mobile.Interfaces;
using EVSwap.Mobile.Models;

namespace EVSwap.Mobile.ViewModels;

public partial class FleetViewModel : BaseViewModel
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private ObservableCollection<FleetVehicleModel> _vehicles = new();

    public FleetViewModel(
        IApiService apiService,
        INavigationService navigationService,
        IConnectivityService connectivityService)
        : base(navigationService, connectivityService)
    {
        _apiService = apiService;
        Title = "Fleet Dashboard";
    }

    [RelayCommand]
    private async Task LoadVehiclesAsync()
    {
        IsBusy = true;
        try
        {
            var vehicles = await _apiService.GetAsync<List<FleetVehicleModel>>("/api/fleet/vehicles");
            if (vehicles is not null)
            {
                Vehicles.Clear();
                foreach (var v in vehicles)
                    Vehicles.Add(v);
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
