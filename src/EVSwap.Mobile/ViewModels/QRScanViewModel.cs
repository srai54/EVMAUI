using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EVSwap.Mobile.Interfaces;

namespace EVSwap.Mobile.ViewModels;

public partial class QRScanViewModel : BaseViewModel
{
    [ObservableProperty]
    private string _manualCode = string.Empty;

    public QRScanViewModel(
        INavigationService navigationService,
        IConnectivityService connectivityService)
        : base(navigationService, connectivityService)
    {
        Title = "Scan QR";
    }

    [RelayCommand]
    private async Task SubmitManualCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(ManualCode))
        {
            await ShowAlertAsync("Validation", "Please enter a QR code.");
            return;
        }

        await ShowAlertAsync("Scanned", $"Code: {ManualCode}");
        await NavigationService.GoBackAsync();
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await NavigationService.GoBackAsync();
    }
}
