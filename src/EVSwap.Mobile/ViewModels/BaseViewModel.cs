using CommunityToolkit.Mvvm.ComponentModel;
using EVSwap.Mobile.Interfaces;

namespace EVSwap.Mobile.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isRefreshing;

    protected INavigationService NavigationService { get; }
    protected IConnectivityService ConnectivityService { get; }

    public BaseViewModel(INavigationService navigationService, IConnectivityService connectivityService)
    {
        NavigationService = navigationService;
        ConnectivityService = connectivityService;
    }

    protected async Task ShowAlertAsync(string title, string message, string cancel = "OK")
    {
        if (Shell.Current?.CurrentPage is not null)
            await Shell.Current.CurrentPage.DisplayAlert(title, message, cancel);
    }
}
