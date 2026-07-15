using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVSwap.API.Core.DTOs.Auth;
using EVSwap.API.Core.Interfaces.Services;

namespace EVSwap.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await _userService.GetProfileAsync(GetUserId());
        return Ok(profile);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var profile = await _userService.UpdateProfileAsync(GetUserId(), request);
        return Ok(profile);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        return Ok(user);
    }

    [HttpPost("assign-role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
    {
        await _userService.AssignRoleAsync(request.UserId, request.Role);
        return Ok(new { message = "Role assigned successfully" });
    }
}
