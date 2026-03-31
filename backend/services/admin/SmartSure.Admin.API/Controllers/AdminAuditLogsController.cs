using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSure.Admin.Application.Interfaces;

namespace SmartSure.Admin.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin/audit-logs")]
public class AdminAuditLogsController : ControllerBase
{
    private readonly IAdminAuditLogService _auditLogService;

    public AdminAuditLogsController(IAdminAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAuditLogs([FromQuery] string? entityType, [FromQuery] Guid? userId, [FromQuery] DateTime? fromDate, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _auditLogService.GetAuditLogsAsync(entityType, userId, fromDate, page, pageSize);
        return Ok(result);
    }
}
