using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSure.Admin.Application.Interfaces;

namespace SmartSure.Admin.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin/dashboard")]
public class AdminDashboardController : ControllerBase
{
    private readonly IAdminDashboardService _dashboardService;

    public AdminDashboardController(IAdminDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboardKpis()
    {
        var result = await _dashboardService.GetDashboardKpisAsync();
        return Ok(result);
    }
}
