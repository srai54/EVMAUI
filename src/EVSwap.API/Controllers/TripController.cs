using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVSwap.API.Core.DTOs.Trip;
using EVSwap.API.Core.Interfaces.Services;

namespace EVSwap.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TripController : ControllerBase
{
    private readonly ITripService _tripService;

    public TripController(ITripService tripService)
    {
        _tripService = tripService;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var trips = await _tripService.GetHistoryAsync(GetUserId());
        return Ok(trips);
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartTrip([FromBody] StartTripRequest request)
    {
        try
        {
            var trip = await _tripService.StartTripAsync(GetUserId(), request);
            return Ok(trip);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/end")]
    public async Task<IActionResult> EndTrip(int id, [FromBody] EndTripRequest request)
    {
        try
        {
            request.TripId = id;
            var trip = await _tripService.EndTripAsync(GetUserId(), request);
            return Ok(trip);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveTrip()
    {
        var trip = await _tripService.GetActiveTripAsync(GetUserId());
        if (trip == null)
            return NotFound(new { message = "No active trip" });
        return Ok(trip);
    }
}
