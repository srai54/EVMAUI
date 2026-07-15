using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVSwap.API.Core.DTOs.Maintenance;
using EVSwap.API.Core.Interfaces.Services;

namespace EVSwap.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MaintenanceController : ControllerBase
{
    private readonly IMaintenanceService _maintenanceService;

    public MaintenanceController(IMaintenanceService maintenanceService)
    {
        _maintenanceService = maintenanceService;
    }

    [HttpGet]
    [Authorize(Roles = "ServiceEngineer,Admin")]
    public async Task<IActionResult> GetRequests()
    {
        var requests = await _maintenanceService.GetRequestsAsync();
        return Ok(requests);
    }

    [HttpPost]
    [Authorize(Roles = "ServiceEngineer,Admin")]
    public async Task<IActionResult> CreateRequest([FromBody] MaintenanceDto dto)
    {
        try
        {
            var result = await _maintenanceService.CreateRequestAsync(dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/resolve")]
    [Authorize(Roles = "ServiceEngineer,Admin")]
    public async Task<IActionResult> ResolveRequest(int id)
    {
        try
        {
            var result = await _maintenanceService.ResolveRequestAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{batteryId}/diagnostics")]
    [Authorize(Roles = "ServiceEngineer,Admin")]
    public async Task<IActionResult> GetDiagnostics(int batteryId)
    {
        var diagnostics = await _maintenanceService.GetDiagnosticsAsync(batteryId);
        return Ok(diagnostics);
    }
}
