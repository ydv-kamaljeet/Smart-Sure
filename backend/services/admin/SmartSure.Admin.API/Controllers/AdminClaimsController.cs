using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSure.Admin.Application.DTOs;
using SmartSure.Admin.Application.Interfaces;

namespace SmartSure.Admin.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin/claims")]
public class AdminClaimsController : ControllerBase
{
    private readonly IAdminClaimsService _claimsService;

    public AdminClaimsController(IAdminClaimsService claimsService)
    {
        _claimsService = claimsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetClaims([FromQuery] string? status, [FromQuery] DateTime? fromDate, [FromQuery] Guid? userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _claimsService.GetClaimsAsync(status, fromDate, userId, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{claimId}")]
    public async Task<IActionResult> GetClaimById(int claimId)
    {
        var result = await _claimsService.GetClaimByIdAsync(claimId);
        return result != null ? Ok(result) : NotFound();
    }

    [HttpPut("{claimId}/review")]
    public async Task<IActionResult> MarkAsUnderReview(int claimId, [FromBody] UpdateClaimStatusDto request)
    {
        var success = await _claimsService.MarkAsUnderReviewAsync(claimId, request.Remarks ?? "");
        return success ? NoContent() : NotFound();
    }

    [HttpPut("{claimId}/approve")]
    public async Task<IActionResult> ApproveClaim(int claimId, [FromBody] UpdateClaimStatusDto request)
    {
        var success = await _claimsService.ApproveClaimAsync(claimId, request.Remarks ?? "");
        return success ? NoContent() : NotFound();
    }

    [HttpPut("{claimId}/reject")]
    public async Task<IActionResult> RejectClaim(int claimId, [FromBody] UpdateClaimStatusDto request)
    {
        var success = await _claimsService.RejectClaimAsync(claimId, request.Remarks ?? "Rejected by Admin");
        return success ? NoContent() : NotFound();
    }
}
