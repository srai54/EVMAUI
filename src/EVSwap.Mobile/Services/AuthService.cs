using System.Text.Json;
using EVSwap.Mobile.Helpers;
using EVSwap.Mobile.Interfaces;
using EVSwap.Mobile.Models;

namespace EVSwap.Mobile.Services;

public class AuthService : IAuthService
{
    private readonly IApiService _apiService;
    private readonly ISecureStorageService _secureStorage;

    public UserModel? CurrentUser { get; private set; }

    public bool IsAuthenticated => CurrentUser is not null;

    public AuthService(IApiService apiService, ISecureStorageService secureStorage)
    {
        _apiService = apiService;
        _secureStorage = secureStorage;
    }

    public async Task<AuthResponse?> LoginAsync(string username, string password)
    {
        var request = new LoginRequest { Username = username, Password = password };
        var response = await _apiService.PostAsync<AuthResponse>("/api/auth/login", request);

        if (response is not null)
        {
            await StoreAuthDataAsync(response);
            CurrentUser = response.User;
        }

        return response;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        var response = await _apiService.PostAsync<AuthResponse>("/api/auth/register", request);

        if (response is not null)
        {
            await StoreAuthDataAsync(response);
            CurrentUser = response.User;
        }

        return response;
    }

    public async Task<AuthResponse?> RefreshTokenAsync()
    {
        var refreshToken = await _secureStorage.GetAsync(Constants.StorageKeys.RefreshToken);
        if (string.IsNullOrEmpty(refreshToken)) return null;

        var response = await _apiService.PostAsync<AuthResponse>("/api/auth/refresh", new { refreshToken });

        if (response is not null)
        {
            await StoreAuthDataAsync(response);
            CurrentUser = response.User;
        }

        return response;
    }

    public async Task LogoutAsync()
    {
        CurrentUser = null;
        _secureStorage.RemoveAll();
        await Task.CompletedTask;
    }

    public async Task<bool> BiometricLoginAsync()
    {
        try
        {
            var result = await Microsoft.Maui.Authentication.WebAuthenticator.Default.AuthenticateAsync(
                new WebAuthenticatorOptions
                {
                    Url = new Uri($"{Constants.ApiBaseUrl}/api/auth/biometric"),
                    CallbackUrl = new Uri("evswap://callback")
                });

            if (!string.IsNullOrEmpty(result?.Properties["token"]))
            {
                var response = new AuthResponse
                {
                    Token = result.Properties["token"],
                    RefreshToken = result.Properties.GetValueOrDefault("refreshToken", string.Empty)
                };

                await StoreAuthDataAsync(response);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private async Task StoreAuthDataAsync(AuthResponse response)
    {
        if (!string.IsNullOrEmpty(response.Token))
            await _secureStorage.SaveAsync(Constants.StorageKeys.AuthToken, response.Token);

        if (!string.IsNullOrEmpty(response.RefreshToken))
            await _secureStorage.SaveAsync(Constants.StorageKeys.RefreshToken, response.RefreshToken);

        if (response.User is not null)
        {
            var userJson = JsonSerializer.Serialize(response.User);
            await _secureStorage.SaveAsync(Constants.StorageKeys.UserKey, userJson);
        }
    }
}
