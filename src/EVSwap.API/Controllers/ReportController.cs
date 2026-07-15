using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVSwap.API.Core.Interfaces.Services;

namespace EVSwap.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("dashboard")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDashboard()
    {
        var dashboard = await _reportService.GetDashboardAsync();
        return Ok(dashboard);
    }

    [HttpGet("user-dashboard")]
    [Authorize]
    public async Task<IActionResult> GetUserDashboard()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var dashboard = await _reportService.GetUserDashboardAsync(userId);
        return Ok(dashboard);
    }
}
