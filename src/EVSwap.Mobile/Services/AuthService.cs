using System.Text.Json;
using EVSwap.Mobile.Helpers;
using EVSwap.Mobile.Interfaces;
using EVSwap.Mobile.Models;

namespace EVSwap.Mobile.Services;

public class AuthService : IAuthService
{
    private readonly IApiService _api;
    private readonly ISecureStorageService _storage;

    public UserModel? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser is not null;

    public AuthService(IApiService api, ISecureStorageService storage)
    {
        _api = api;
        _storage = storage;
    }

    public async Task<AuthResponse?> LoginAsync(string username, string password)
    {
        var request = new LoginRequest { Username = username, Password = password };
        var response = await _api.PostAsync<AuthResponse>("/api/auth/login", request);

        if (response is not null)
        {
            await SaveAuthData(response);
            CurrentUser = response.User;
        }

        return response;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        var response = await _api.PostAsync<AuthResponse>("/api/auth/register", request);

        if (response is not null)
        {
            await SaveAuthData(response);
            CurrentUser = response.User;
        }

        return response;
    }

    public async Task<AuthResponse?> RefreshTokenAsync()
    {
        var refreshToken = await _storage.GetAsync(Constants.StorageKeys.RefreshToken);
        if (string.IsNullOrEmpty(refreshToken)) return null;

        var response = await _api.PostAsync<AuthResponse>("/api/auth/refresh", new { refreshToken });

        if (response is not null)
        {
            await SaveAuthData(response);
            CurrentUser = response.User;
        }

        return response;
    }

    public void BypassLogin()
    {
        CurrentUser = new UserModel
        {
            Id = 1,
            Username = "admin",
            Email = "admin@evswap.com",
            Phone = "0000000000",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Roles = new List<string> { "Admin" }
        };
    }

    public async Task LogoutAsync()
    {
        CurrentUser = null;
        _storage.RemoveAll();
    }

    private async Task SaveAuthData(AuthResponse response)
    {
        if (!string.IsNullOrEmpty(response.Token))
            await _storage.SaveAsync(Constants.StorageKeys.AuthToken, response.Token);

        if (!string.IsNullOrEmpty(response.RefreshToken))
            await _storage.SaveAsync(Constants.StorageKeys.RefreshToken, response.RefreshToken);

        if (response.User is not null)
        {
            var json = JsonSerializer.Serialize(response.User);
            await _storage.SaveAsync(Constants.StorageKeys.UserKey, json);
        }
    }
}