using EVSwap.API.Core.Interfaces.Services;
using EVSwap.API.Core.DTOs.Auth;
using EVSwap.API.Core.Entities;
using EVSwap.API.Core.Interfaces.Repositories;

namespace EVSwap.API.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserProfileDto> GetProfileAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found");
        var roles = (await _userRepository.GetUserRolesAsync(userId)).ToList();
        return MapToProfile(user, roles);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found");

        if (!string.IsNullOrEmpty(request.Phone)) user.Phone = request.Phone;
        if (!string.IsNullOrEmpty(request.Email)) user.Email = request.Email;

        await _userRepository.UpdateAsync(user);
        var roles = (await _userRepository.GetUserRolesAsync(userId)).ToList();
        return MapToProfile(user, roles);
    }

    public async Task<IEnumerable<UserProfileDto>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();
        var result = new List<UserProfileDto>();
        foreach (var user in users)
        {
            var roles = (await _userRepository.GetUserRolesAsync(user.Id)).ToList();
            result.Add(MapToProfile(user, roles));
        }
        return result;
    }

    public async Task<UserProfileDto> GetUserByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("User not found");
        var roles = (await _userRepository.GetUserRolesAsync(id)).ToList();
        return MapToProfile(user, roles);
    }

    public async Task AssignRoleAsync(int userId, string role)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found");

        var roleEntity = await _userRepository.FindAsync(x => false);
        await _userRepository.UpdateAsync(user);
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
