using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSure.Admin.Application.DTOs;
using SmartSure.Admin.Application.Interfaces;
using SmartSure.Admin.API.Services;

namespace SmartSure.Admin.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin/reports")]
public class AdminReportsController : ControllerBase
{
    private readonly IAdminReportsService _reportsService;
    private readonly IAdminClaimsService _claimsService;
    private readonly IAdminPolicyService _policyService;
    private readonly IAdminUsersService _usersService;
    private readonly IAdminAuditLogService _auditLogService;

    public AdminReportsController(
        IAdminReportsService reportsService,
        IAdminClaimsService claimsService,
        IAdminPolicyService policyService,
        IAdminUsersService usersService,
        IAdminAuditLogService auditLogService)
    {
        _reportsService = reportsService;
        _claimsService = claimsService;
        _policyService = policyService;
        _usersService = usersService;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetReports([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _reportsService.GetReportsAsync(page, pageSize);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateReport([FromBody] CreateReportRequest request)
    {
        var result = await _reportsService.TriggerReportGenerationAsync(request);
        return AcceptedAtAction(nameof(GetReportById), new { reportId = result.Id }, result);
    }

    [HttpGet("{reportId}")]
    public async Task<IActionResult> GetReportById(Guid reportId)
    {
        var result = await _reportsService.GetReportByIdAsync(reportId);
        return result != null ? Ok(result) : NotFound();
    }

    // POST /api/admin/reports/generate — generates PDF and streams it back
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateReport([FromBody] GenerateReportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Type))
            return BadRequest(new { errorMessage = "Report type is required." });

        var title = string.IsNullOrWhiteSpace(request.Title) ? $"{request.Type} Report" : request.Title;
        byte[] pdf;

        switch (request.Type.ToLower())
        {
            case "claims":
                var claims = await _claimsService.GetClaimsAsync(null, null, null, 1, 10000);
                pdf = ReportPdfGenerator.GenerateClaimsReport(claims.Items, title);
                break;
            case "policies":
                var policies = await _policyService.GetPoliciesAsync(null, null, 1, 10000);
                pdf = ReportPdfGenerator.GeneratePoliciesReport(policies.Items, title);
                break;
            case "revenue":
                var revPolicies = await _policyService.GetPoliciesAsync(null, null, 1, 10000);
                pdf = ReportPdfGenerator.GenerateRevenueReport(revPolicies.Items, title);
                break;
            case "audit":
                var logs = await _auditLogService.GetAuditLogsAsync(null, null, null, 1, 10000);
                pdf = ReportPdfGenerator.GenerateAuditReport(logs.Items, title);
                break;
            default:
                return BadRequest(new { errorMessage = $"Unknown report type: {request.Type}" });
        }

        var fileName = $"{request.Type}-report-{DateTime.UtcNow:yyyyMMdd-HHmm}.pdf";
        return File(pdf, "application/pdf", fileName);
    }
}

public class GenerateReportRequest
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}
