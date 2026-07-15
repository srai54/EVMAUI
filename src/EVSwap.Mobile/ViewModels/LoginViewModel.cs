using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EVSwap.Mobile.Helpers;
using EVSwap.Mobile.Interfaces;

namespace EVSwap.Mobile.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly ISecureStorageService _secureStorage;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isBiometricAvailable;

    public LoginViewModel(
        IAuthService authService,
        ISecureStorageService secureStorage,
        INavigationService navigationService,
        IConnectivityService connectivityService)
        : base(navigationService, connectivityService)
    {
        _authService = authService;
        _secureStorage = secureStorage;
        Title = "Login";
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            await ShowAlertAsync("Validation Error", "Please enter username and password.");
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _authService.LoginAsync(Username, Password);
            if (result is not null)
            {
                await NavigationService.NavigateToAsync($"//{Constants.Routes.Dashboard}");
            }
            else
            {
                await ShowAlertAsync("Login Failed", "Invalid credentials.");
            }
        }
        catch (Exception ex)
        {
            await ShowAlertAsync("Error", $"Login failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        await NavigationService.NavigateToAsync(Constants.Routes.Register);
    }

    [RelayCommand]
    private async Task BiometricLoginAsync()
    {
        IsBusy = true;
        try
        {
            var success = await _authService.BiometricLoginAsync();
            if (success)
            {
                await NavigationService.NavigateToAsync($"//{Constants.Routes.Dashboard}");
            }
            else
            {
                await ShowAlertAsync("Biometric Login", "Authentication failed.");
            }
        }
        catch (Exception ex)
        {
            await ShowAlertAsync("Error", $"Biometric login failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ForgotPasswordAsync()
    {
        await NavigationService.NavigateToAsync(Constants.Routes.ForgotPassword);
    }

    [RelayCommand]
    private async Task BypassLoginAsync()
    {
        IsBusy = true;
        try
        {
            _authService.BypassLogin();
            await NavigationService.NavigateToAsync($"//{Constants.Routes.Dashboard}");
        }
        catch (Exception ex)
        {
            await ShowAlertAsync("Error", $"Bypass failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
