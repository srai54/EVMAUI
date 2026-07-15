namespace EVSwap.Mobile.Models;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserModel? User { get; set; }
    public List<string> Roles { get; set; } = new();
}
