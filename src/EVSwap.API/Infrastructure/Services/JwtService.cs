using EVSwap.API.Core.Interfaces.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EVSwap.API.Core.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using EVSwap.API.Infrastructure.Utilities;

namespace EVSwap.API.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly AppConstants _constants;

    public JwtService(IOptions<AppConstants> constants)
    {
        _constants = constants.Value;
    }

    public (string token, DateTime expires) GenerateToken(User user, IList<string> roles)
    {
        var expires = DateTime.UtcNow.AddMinutes(_constants.JwtExpiryMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_constants.JwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: _constants.JwtIssuer,
            audience: _constants.JwtAudience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_constants.JwtKey));

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _constants.JwtIssuer,
                ValidateAudience = true,
                ValidAudience = _constants.JwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
