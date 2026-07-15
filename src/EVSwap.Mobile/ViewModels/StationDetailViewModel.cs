using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EVSwap.Mobile.Helpers;
using EVSwap.Mobile.Interfaces;
using EVSwap.Mobile.Models;

namespace EVSwap.Mobile.ViewModels;

[QueryProperty(nameof(Station), "Station")]
public partial class StationDetailViewModel : BaseViewModel
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private StationModel? _station;

    [ObservableProperty]
    private ObservableCollection<BatteryModel> _availableBatteries = new();

    public StationDetailViewModel(
        IApiService apiService,
        INavigationService navigationService,
        IConnectivityService connectivityService)
        : base(navigationService, connectivityService)
    {
        _apiService = apiService;
    }

    partial void OnStationChanged(StationModel? value)
    {
        if (value is not null)
        {
            Title = value.Name;
            LoadBatteriesCommand.ExecuteAsync(null).FireAndForget();
        }
    }

    [RelayCommand]
    private async Task LoadBatteriesAsync()
    {
        if (Station is null) return;
        IsBusy = true;
        try
        {
            var batteries = await _apiService.GetAsync<List<BatteryModel>>($"/api/station/{Station.Id}/batteries");
            if (batteries is not null)
            {
                AvailableBatteries.Clear();
                foreach (var b in batteries)
                    AvailableBatteries.Add(b);
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
        if (Station is null) return;
        var parameters = new Dictionary<string, object> { { "Station", Station } };
        await NavigationService.NavigateToAsync(Constants.Routes.SwapRequest, parameters);
    }
}
