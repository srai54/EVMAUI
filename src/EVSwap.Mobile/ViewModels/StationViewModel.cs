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
            Stations.Clear();
            Stations.Add(new StationModel { Id = 1, Name = "Central Station", Address = "123 Main St, Downtown", Latitude = 40.7128, Longitude = -74.0060, Status = "Active", DistanceKm = 1.2 });
            Stations.Add(new StationModel { Id = 2, Name = "East Side Hub", Address = "456 East Ave", Latitude = 40.7150, Longitude = -73.9900, Status = "Active", DistanceKm = 2.5 });
            Stations.Add(new StationModel { Id = 3, Name = "West End Station", Address = "789 West Blvd", Latitude = 40.7100, Longitude = -74.0200, Status = "Active", DistanceKm = 3.8 });
            Stations.Add(new StationModel { Id = 4, Name = "North Point", Address = "321 North Rd", Latitude = 40.7200, Longitude = -74.0050, Status = "Maintenance", DistanceKm = 4.1 });
            Stations.Add(new StationModel { Id = 5, Name = "South Park Station", Address = "654 South St", Latitude = 40.7050, Longitude = -74.0080, Status = "Active", DistanceKm = 5.3 });
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
