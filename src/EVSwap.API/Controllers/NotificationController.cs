using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVSwap.API.Core.Interfaces.Services;

namespace EVSwap.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var notifications = await _notificationService.GetNotificationsAsync(GetUserId());
        return Ok(notifications);
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        try
        {
            await _notificationService.MarkAsReadAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
