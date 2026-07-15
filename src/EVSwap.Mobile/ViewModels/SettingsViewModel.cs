using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EVSwap.Mobile.Helpers;
using EVSwap.Mobile.Interfaces;

namespace EVSwap.Mobile.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly ISecureStorageService _secureStorage;

    [ObservableProperty]
    private bool _isBiometricEnabled;

    [ObservableProperty]
    private bool _notificationsEnabled = true;

    [ObservableProperty]
    private string _appVersion = "1.0.0";

    public SettingsViewModel(
        ISecureStorageService secureStorage,
        INavigationService navigationService,
        IConnectivityService connectivityService)
        : base(navigationService, connectivityService)
    {
        _secureStorage = secureStorage;
        Title = "Settings";
    }

    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
        var bio = await _secureStorage.GetAsync(Constants.StorageKeys.BiometricEnabled);
        IsBiometricEnabled = bio == "true";

        var notif = await _secureStorage.GetAsync(Constants.StorageKeys.NotificationsEnabled);
        NotificationsEnabled = notif != "false";
    }

    partial void OnIsBiometricEnabledChanged(bool value)
    {
        _secureStorage.SaveAsync(Constants.StorageKeys.BiometricEnabled, value.ToString()).FireAndForget();
    }

    partial void OnNotificationsEnabledChanged(bool value)
    {
        _secureStorage.SaveAsync(Constants.StorageKeys.NotificationsEnabled, value.ToString()).FireAndForget();
    }
}
