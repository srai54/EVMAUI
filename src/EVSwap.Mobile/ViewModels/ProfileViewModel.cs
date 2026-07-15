using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EVSwap.Mobile.Helpers;
using EVSwap.Mobile.Interfaces;

namespace EVSwap.Mobile.ViewModels;

public partial class ProfileViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    public ProfileViewModel(
        IApiService apiService,
        IAuthService authService,
        INavigationService navigationService,
        IConnectivityService connectivityService)
        : base(navigationService, connectivityService)
    {
        _apiService = apiService;
        _authService = authService;
        Title = "Profile";
    }

    [RelayCommand]
    private async Task LoadProfileAsync()
    {
        IsBusy = true;
        try
        {
            var user = _authService.CurrentUser;
            if (user is not null)
            {
                UserName = user.Username;
                Email = user.Email;
                Phone = user.Phone;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task UpdateProfileAsync()
    {
        IsBusy = true;
        try
        {
            await _apiService.PutAsync<object>("/api/user/profile", new
            {
                Username = UserName,
                Email = Email,
                Phone = Phone
            });
            await ShowAlertAsync("Success", "Profile updated.");
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
    private async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        await NavigationService.NavigateToAsync($"//{Constants.Routes.Login}");
    }

    [RelayCommand]
    private async Task GoToSettingsAsync()
    {
        await NavigationService.NavigateToAsync(Constants.Routes.Settings);
    }
}
