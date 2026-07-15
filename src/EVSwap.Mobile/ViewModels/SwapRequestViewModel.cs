using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EVSwap.Mobile.Interfaces;
using EVSwap.Mobile.Models;

namespace EVSwap.Mobile.ViewModels;

[QueryProperty(nameof(SelectedStation), "Station")]
public partial class SwapRequestViewModel : BaseViewModel
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private StationModel? _selectedStation;

    [ObservableProperty]
    private ObservableCollection<StationModel> _stations = new();

    [ObservableProperty]
    private StationModel? _station;

    [ObservableProperty]
    private int _vehicleId;

    [ObservableProperty]
    private string _batteryType = "Standard";

    [ObservableProperty]
    private ObservableCollection<string> _batteryTypes = new() { "Standard", "Premium", "LongRange" };

    public SwapRequestViewModel(
        IApiService apiService,
        INavigationService navigationService,
        IConnectivityService connectivityService)
        : base(navigationService, connectivityService)
    {
        _apiService = apiService;
        Title = "Request Swap";
    }

    [RelayCommand]
    private async Task LoadStationsAsync()
    {
        try
        {
            var stations = await _apiService.GetAsync<List<StationModel>>("/api/station");
            if (stations is not null)
            {
                Stations.Clear();
                foreach (var s in stations)
                    Stations.Add(s);
            }
        }
        catch
        {
        }
    }

    [RelayCommand]
    private async Task SubmitRequestAsync()
    {
        if (Station is null)
        {
            await ShowAlertAsync("Validation", "Please select a station.");
            return;
        }

        IsBusy = true;
        try
        {
            var request = new SwapRequestModel
            {
                StationId = Station.Id,
                VehicleId = VehicleId,
                RequestedBatteryType = BatteryType
            };

            var result = await _apiService.PostAsync<SwapRequestModel>("/api/swap/request", request);
            if (result is not null)
            {
                await ShowAlertAsync("Success", "Swap request submitted.");
                await NavigationService.GoBackAsync();
            }
        }
        catch (Exception ex)
        {
            await ShowAlertAsync("Error", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
