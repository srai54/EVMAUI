using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EVSwap.Mobile.Interfaces;
using EVSwap.Mobile.Models;

namespace EVSwap.Mobile.ViewModels;

public partial class TripViewModel : BaseViewModel
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private ObservableCollection<TripModel> _trips = new();

    [ObservableProperty]
    private TripModel? _currentTrip;

    [ObservableProperty]
    private bool _hasActiveTrip;

    public TripViewModel(
        IApiService apiService,
        INavigationService navigationService,
        IConnectivityService connectivityService)
        : base(navigationService, connectivityService)
    {
        _apiService = apiService;
        Title = "Trips";
    }

    [RelayCommand]
    private async Task LoadTripsAsync()
    {
        IsBusy = true;
        try
        {
            var trips = await _apiService.GetAsync<List<TripModel>>("/api/trip");
            if (trips is not null)
            {
                Trips.Clear();
                foreach (var t in trips)
                    Trips.Add(t);
            }

            CurrentTrip = await _apiService.GetAsync<TripModel>("/api/trip/active");
            HasActiveTrip = CurrentTrip is not null;
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
    private async Task StartTripAsync()
    {
        IsBusy = true;
        try
        {
            CurrentTrip = await _apiService.PostAsync<TripModel>("/api/trip/start");
            HasActiveTrip = true;
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

    [RelayCommand]
    private async Task EndTripAsync()
    {
        IsBusy = true;
        try
        {
            await _apiService.PostAsync<object>("/api/trip/end");
            CurrentTrip = null;
            HasActiveTrip = false;
            await LoadTripsAsync();
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
