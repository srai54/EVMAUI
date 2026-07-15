namespace EVSwap.API.Core.DTOs.Auth;

public class AssignRoleRequest
{
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty;
}
