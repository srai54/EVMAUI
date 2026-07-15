using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVSwap.API.Core.DTOs.Battery;
using EVSwap.API.Core.Interfaces.Services;

namespace EVSwap.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BatteryController : ControllerBase
{
    private readonly IBatteryService _batteryService;

    public BatteryController(IBatteryService batteryService)
    {
        _batteryService = batteryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var batteries = await _batteryService.GetAllAsync();
        return Ok(batteries);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var battery = await _batteryService.GetByIdAsync(id);
            return Ok(battery);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("nearby/{stationId}")]
    public async Task<IActionResult> GetNearby(int stationId)
    {
        var batteries = await _batteryService.GetNearbyAsync(stationId);
        return Ok(batteries);
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin,StationOperator")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
    {
        try
        {
            var battery = await _batteryService.UpdateStatusAsync(id, status);
            return Ok(battery);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/health")]
    public async Task<IActionResult> RecordHealth(int id, [FromBody] BatteryHealthDto healthDto)
    {
        try
        {
            var result = await _batteryService.RecordHealthAsync(id, healthDto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
