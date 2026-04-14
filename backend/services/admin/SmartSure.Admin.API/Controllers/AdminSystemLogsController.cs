using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSure.Admin.Application.DTOs;
using SmartSure.Admin.Application.Interfaces;

namespace SmartSure.Admin.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin/system-logs")]
public class AdminSystemLogsController : ControllerBase
{
    private readonly ISystemLogService _logService;

    public AdminSystemLogsController(ISystemLogService logService)
    {
        _logService = logService;
    }

    [HttpGet("search")]
    public async Task<ActionResult<PagedLogResultDto>> SearchLogs([FromQuery] LogSearchParametersDto parameters)
    {
        var logs = await _logService.SearchLogsAsync(parameters);
        return Ok(logs);
    }

    [HttpGet("metadata")]
    public async Task<ActionResult<LogMetadataDto>> GetMetadata()
    {
        var metadata = await _logService.GetLogMetadataAsync();
        return Ok(metadata);
    }

    [HttpGet("debug-path")]
    [AllowAnonymous]
    public IActionResult DebugPath()
    {
        var files = Directory.Exists(AppContext.BaseDirectory) 
            ? Directory.GetFiles(AppContext.BaseDirectory, "*.dll").Length 
            : -1;
        return Ok(new { 
            baseDir = AppContext.BaseDirectory,
            dllCount = files
        });
    }
}
