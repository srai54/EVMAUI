using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EVSwap.Mobile.Helpers;
using EVSwap.Mobile.Interfaces;
using EVSwap.Mobile.Models;

namespace EVSwap.Mobile.ViewModels;

public partial class StationViewModel : BaseViewModel
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private ObservableCollection<StationModel> _stations = new();

    [ObservableProperty]
    private StationModel? _selectedStation;

    public StationViewModel(
        IApiService apiService,
        INavigationService navigationService,
        IConnectivityService connectivityService)
        : base(navigationService, connectivityService)
    {
        _apiService = apiService;
        Title = "Stations";
    }

    [RelayCommand]
    private async Task LoadStationsAsync()
    {
        IsBusy = true;
        try
        {
            var stations = await _apiService.GetAsync<List<StationModel>>("/api/station/nearby");
            if (stations is not null)
            {
                Stations.Clear();
                foreach (var station in stations)
                    Stations.Add(station);
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
    private async Task RefreshStationsAsync()
    {
        await LoadStationsAsync();
    }

    [RelayCommand]
    private async Task NavigateToStationAsync(StationModel station)
    {
        if (station is null) return;
        var parameters = new Dictionary<string, object> { { "Station", station } };
        await NavigationService.NavigateToAsync(Constants.Routes.StationDetail, parameters);
    }
}
