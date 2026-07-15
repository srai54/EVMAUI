using EVSwap.API.Core.Interfaces.Services;
using EVSwap.API.Core.DTOs.Auth;
using EVSwap.API.Core.Entities;
using EVSwap.API.Core.Interfaces.Repositories;

namespace EVSwap.API.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IWalletRepository _walletRepository;
    private static readonly Dictionary<string, (string otp, DateTime expires)> _otpStore = new();

    public AuthService(IUserRepository userRepository, IJwtService jwtService, IWalletRepository walletRepository)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _walletRepository = walletRepository;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.FindByUsernameAsync(request.Username)
                   ?? await _userRepository.FindByEmailAsync(request.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated");

        var roles = (await _userRepository.GetUserRolesAsync(user.Id)).ToList();
        var (token, expires) = _jwtService.GenerateToken(user, roles);

        user.RefreshToken = _jwtService.GenerateRefreshToken();
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _userRepository.UpdateAsync(user);

        return new AuthResponse
        {
            Token = token,
            RefreshToken = user.RefreshToken,
            ExpiresAt = expires,
            User = MapToProfile(user, roles),
            Roles = roles
        };
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _userRepository.FindByUsernameAsync(request.Username) != null)
            throw new InvalidOperationException("Username already exists");

        if (await _userRepository.FindByEmailAsync(request.Email) != null)
            throw new InvalidOperationException("Email already exists");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Phone = request.Phone,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        user = await _userRepository.AddAsync(user);

        var riderRole = "EVRider";
        var userRoles = await _userRepository.GetUserRolesAsync(user.Id);
        var roles = new List<string> { riderRole };

        var wallet = new Wallet { UserId = user.Id, Balance = 0 };
        await _walletRepository.AddAsync(wallet);

        var (token, expires) = _jwtService.GenerateToken(user, roles);

        user.RefreshToken = _jwtService.GenerateRefreshToken();
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _userRepository.UpdateAsync(user);

        return new AuthResponse
        {
            Token = token,
            RefreshToken = user.RefreshToken,
            ExpiresAt = expires,
            User = MapToProfile(user, roles),
            Roles = roles
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var user = (await _userRepository.GetAllAsync())
            .FirstOrDefault(u => u.RefreshToken == request.RefreshToken);

        if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Invalid or expired refresh token");

        var roles = (await _userRepository.GetUserRolesAsync(user.Id)).ToList();
        var (token, expires) = _jwtService.GenerateToken(user, roles);

        user.RefreshToken = _jwtService.GenerateRefreshToken();
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _userRepository.UpdateAsync(user);

        return new AuthResponse
        {
            Token = token,
            RefreshToken = user.RefreshToken,
            ExpiresAt = expires,
            User = MapToProfile(user, roles),
            Roles = roles
        };
    }

    public Task<string> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var otp = Random.Shared.Next(100000, 999999).ToString();
        _otpStore[request.Email] = (otp, DateTime.UtcNow.AddMinutes(10));
        return Task.FromResult(otp);
    }

    public Task<bool> VerifyOtpAsync(VerifyOtpRequest request)
    {
        if (_otpStore.TryGetValue(request.Email, out var stored))
        {
            if (stored.expires > DateTime.UtcNow && stored.otp == request.Otp)
            {
                _otpStore.Remove(request.Email);
                return Task.FromResult(true);
            }
        }
        return Task.FromResult(false);
    }

    private static UserProfileDto MapToProfile(User user, List<string> roles) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        Phone = user.Phone,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        Roles = roles
    };
}
