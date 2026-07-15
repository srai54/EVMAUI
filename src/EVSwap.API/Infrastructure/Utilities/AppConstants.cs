namespace EVSwap.API.Infrastructure.Utilities;

public class AppConstants
{
    public const string SectionName = "AppConstants";

    public string JwtKey { get; set; } = "ThisIsASecretKeyForJwtTokenGenerationThatIsAtLeast32Chars!";
    public string JwtIssuer { get; set; } = "EVSwap";
    public string JwtAudience { get; set; } = "EVSwapMobile";
    public int JwtExpiryMinutes { get; set; } = 60;
}
