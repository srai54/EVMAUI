using EVSwap.API.Core.Interfaces.Services;
namespace EVSwap.API.Infrastructure.Services;

public class EmailService : IEmailService
{
    public Task SendOtpAsync(string email, string otp)
    {
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string email, string resetLink)
    {
        return Task.CompletedTask;
    }
}
