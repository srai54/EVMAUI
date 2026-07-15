using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EVSwap.Mobile.Interfaces;
using EVSwap.Mobile.Validators;

namespace EVSwap.Mobile.ViewModels;

public partial class ForgotPasswordViewModel : BaseViewModel
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _otp = string.Empty;

    [ObservableProperty]
    private bool _otpSent;

    [ObservableProperty]
    private bool _otpVerified;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    public ForgotPasswordViewModel(
        IApiService apiService,
        INavigationService navigationService,
        IConnectivityService connectivityService)
        : base(navigationService, connectivityService)
    {
        _apiService = apiService;
        Title = "Forgot Password";
    }

    [RelayCommand]
    private async Task SendOtpAsync()
    {
        if (!Validators.Validators.IsValidEmail(Email))
        {
            await ShowAlertAsync("Validation Error", "Please enter a valid email.");
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _apiService.PostAsync<object>("/api/auth/forgot-password", new { Email });
            OtpSent = true;
            await ShowAlertAsync("OTP Sent", "Please check your email for the OTP.");
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
    private async Task VerifyOtpAsync()
    {
        if (string.IsNullOrWhiteSpace(Otp))
        {
            await ShowAlertAsync("Validation Error", "Please enter the OTP.");
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _apiService.PostAsync<object>("/api/auth/verify-otp", new { Email, Otp });
            OtpVerified = true;
            await ShowAlertAsync("Verified", "OTP verified. You can now reset your password.");
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
    private async Task ResetPasswordAsync()
    {
        if (!Validators.Validators.IsValidPassword(NewPassword))
        {
            await ShowAlertAsync("Validation Error", "Password must be at least 6 characters.");
            return;
        }

        IsBusy = true;
        try
        {
            await _apiService.PostAsync<object>("/api/auth/reset-password", new { Email, Otp, NewPassword });
            await ShowAlertAsync("Success", "Password reset successfully.");
            await NavigationService.GoBackAsync();
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
