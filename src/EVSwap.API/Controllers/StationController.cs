using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVSwap.API.Core.DTOs.Station;
using EVSwap.API.Core.Interfaces.Services;

namespace EVSwap.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StationController : ControllerBase
{
    private readonly IStationService _stationService;

    public StationController(IStationService stationService)
    {
        _stationService = stationService;
    }

    [HttpGet("nearby")]
    public async Task<IActionResult> GetNearby([FromQuery] NearbyStationQuery query)
    {
        var stations = await _stationService.GetNearbyAsync(query);
        return Ok(stations);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var station = await _stationService.GetByIdAsync(id);
            return Ok(station);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] StationDto stationDto)
    {
        var station = await _stationService.CreateAsync(stationDto);
        return CreatedAtAction(nameof(GetById), new { id = station.Id }, station);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] StationDto stationDto)
    {
        try
        {
            var station = await _stationService.UpdateAsync(id, stationDto);
            return Ok(station);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _stationService.DeleteAsync(id);
        return NoContent();
    }
}
