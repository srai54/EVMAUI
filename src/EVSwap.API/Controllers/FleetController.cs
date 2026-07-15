using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVSwap.API.Core.DTOs.Fleet;
using EVSwap.API.Core.Interfaces.Services;

namespace EVSwap.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FleetController : ControllerBase
{
    private readonly IFleetService _fleetService;

    public FleetController(IFleetService fleetService)
    {
        _fleetService = fleetService;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [Authorize(Roles = "FleetManager,Admin")]
    public async Task<IActionResult> GetVehicles()
    {
        var vehicles = await _fleetService.GetVehiclesAsync(GetUserId());
        return Ok(vehicles);
    }

    [HttpPost("assign")]
    [Authorize(Roles = "FleetManager,Admin")]
    public async Task<IActionResult> AssignDriver([FromBody] FleetDto fleetDto)
    {
        var result = await _fleetService.AssignDriverAsync(GetUserId(), fleetDto);
        return Ok(result);
    }
}
