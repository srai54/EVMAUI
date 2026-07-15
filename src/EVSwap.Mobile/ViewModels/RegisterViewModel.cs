using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EVSwap.Mobile.Helpers;
using EVSwap.Mobile.Interfaces;
using EVSwap.Mobile.Models;
using EVSwap.Mobile.Validators;

namespace EVSwap.Mobile.ViewModels;

public partial class RegisterViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    public RegisterViewModel(
        IAuthService authService,
        INavigationService navigationService,
        IConnectivityService connectivityService)
        : base(navigationService, connectivityService)
    {
        _authService = authService;
        Title = "Register";
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            await ShowAlertAsync("Validation Error", "All fields are required.");
            return;
        }

        if (!Validators.Validators.IsValidEmail(Email))
        {
            await ShowAlertAsync("Validation Error", "Invalid email address.");
            return;
        }

        if (!Validators.Validators.IsValidPassword(Password))
        {
            await ShowAlertAsync("Validation Error", "Password must be at least 6 characters.");
            return;
        }

        if (Password != ConfirmPassword)
        {
            await ShowAlertAsync("Validation Error", "Passwords do not match.");
            return;
        }

        IsBusy = true;
        try
        {
            var request = new RegisterRequest
            {
                Username = Username,
                Email = Email,
                Password = Password,
                Phone = Phone
            };

            var result = await _authService.RegisterAsync(request);
            if (result is not null)
            {
                await NavigationService.NavigateToAsync($"//{Constants.Routes.Dashboard}");
            }
            else
            {
                await ShowAlertAsync("Registration Failed", "Could not create account.");
            }
        }
        catch (Exception ex)
        {
            await ShowAlertAsync("Error", $"Registration failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoToLoginAsync()
    {
        await NavigationService.GoBackAsync();
    }
}
