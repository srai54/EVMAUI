namespace EVSwap.API.Core.Interfaces.Services;

public interface IEmailService
{
    Task SendOtpAsync(string email, string otp);
    Task SendPasswordResetAsync(string email, string resetLink);
}
