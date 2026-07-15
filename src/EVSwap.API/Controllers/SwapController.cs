using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVSwap.API.Core.DTOs.Swap;
using EVSwap.API.Core.Interfaces.Services;

namespace EVSwap.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SwapController : ControllerBase
{
    private readonly ISwapService _swapService;

    public SwapController(ISwapService swapService)
    {
        _swapService = swapService;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("request")]
    [Authorize(Roles = "EVRider,Admin")]
    public async Task<IActionResult> RequestSwap([FromBody] SwapRequestDto request)
    {
        try
        {
            var result = await _swapService.RequestSwapAsync(GetUserId(), request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/approve")]
    [Authorize(Roles = "StationOperator,Admin")]
    public async Task<IActionResult> ApproveSwap(int id, [FromBody] int newBatteryId)
    {
        try
        {
            var result = await _swapService.ApproveSwapAsync(id, newBatteryId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var history = await _swapService.GetHistoryAsync(GetUserId());
        return Ok(history);
    }

    [HttpGet("pending")]
    [Authorize(Roles = "StationOperator,Admin")]
    public async Task<IActionResult> GetPendingRequests()
    {
        var requests = await _swapService.GetPendingRequestsAsync();
        return Ok(requests);
    }
}
