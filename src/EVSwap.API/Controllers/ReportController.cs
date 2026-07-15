using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVSwap.API.Core.Interfaces.Services;

namespace EVSwap.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var dashboard = await _reportService.GetDashboardAsync();
        return Ok(dashboard);
    }
}
