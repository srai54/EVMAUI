using EVSwap.API.Core.DTOs.Auth;

namespace EVSwap.API.Core.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task<string> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<bool> VerifyOtpAsync(VerifyOtpRequest request);
}
