using EVSwap.API.Core.DTOs.Auth;

namespace EVSwap.API.Core.Interfaces.Services;

public interface IUserService
{
    Task<UserProfileDto> GetProfileAsync(int userId);
    Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileRequest request);
    Task<IEnumerable<UserProfileDto>> GetAllUsersAsync();
    Task<UserProfileDto> GetUserByIdAsync(int id);
    Task AssignRoleAsync(int userId, string role);
}
