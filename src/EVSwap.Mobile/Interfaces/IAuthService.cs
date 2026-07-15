using EVSwap.Mobile.Models;

namespace EVSwap.Mobile.Interfaces;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(string username, string password);
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> RefreshTokenAsync();
    Task LogoutAsync();
    bool IsAuthenticated { get; }
    UserModel? CurrentUser { get; }
    Task<bool> BiometricLoginAsync();
    void BypassLogin();
}
