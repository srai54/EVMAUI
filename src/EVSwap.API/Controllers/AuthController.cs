using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVSwap.API.Core.DTOs.Auth;
using EVSwap.API.Core.Interfaces.Services;

namespace EVSwap.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var response = await _authService.RegisterAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var response = await _authService.RefreshTokenAsync(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var otp = await _authService.ForgotPasswordAsync(request);
        return Ok(new { otp, message = "OTP sent to email" });
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        var isValid = await _authService.VerifyOtpAsync(request);
        if (!isValid)
            return BadRequest(new { message = "Invalid or expired OTP" });
        return Ok(new { message = "OTP verified successfully" });
    }
}
