using System.Security.Claims;
using EVSwap.API.Core.Entities;

namespace EVSwap.API.Core.Interfaces.Services;

public interface IJwtService
{
    (string token, DateTime expires) GenerateToken(User user, IList<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
}
