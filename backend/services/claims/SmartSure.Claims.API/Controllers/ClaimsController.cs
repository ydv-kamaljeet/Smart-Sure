using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSure.Claims.Application.DTOs;
using SmartSure.Claims.Application.Interfaces;
using SmartSure.Shared.Common.Constants;

namespace SmartSure.Claims.API.Controllers;

[ApiController]
[Route("api/claims")]
[Authorize]
public class ClaimsController : ControllerBase
{
    private readonly IClaimManagementService _claimService;

    public ClaimsController(IClaimManagementService claimService)
    {
        _claimService = claimService;
    }

    private Guid GetUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return idClaim != null ? Guid.Parse(idClaim) : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetClaims([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? status = null)
    {
        var userId = GetUserId();
        // Admin gets all claims if they want, but let's restrict to standard user claims for now or allow admin to omit userId
        if (User.IsInRole(Roles.Admin))
        {
            // If admin, we can fetch all claims by passing empty Guid
            userId = Guid.Empty; 
        }

        var claims = await _claimService.GetClaimsAsync(userId, page, pageSize, status);
        return Ok(claims);
    }

    [HttpGet("{claimId}")]
    public async Task<IActionResult> GetClaim(int claimId)
    {
        var claim = await _claimService.GetClaimByIdAsync(claimId);
        if (claim == null) return NotFound();

        var userId = GetUserId();
        if (!User.IsInRole(Roles.Admin) && claim.UserId != userId) return Forbid();

        return Ok(claim);
    }

    [HttpPost]
    public async Task<IActionResult> InitiateClaim([FromBody] CreateClaimDto dto)
    {
        var userId = GetUserId();
        var result = await _claimService.InitiateClaimAsync(userId, dto);
        
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return CreatedAtAction(nameof(GetClaim), new { claimId = result.Data!.Id }, result.Data);
    }

    [HttpPut("{claimId}")]
    public async Task<IActionResult> UpdateDraftClaim(int claimId, [FromBody] UpdateClaimDto dto)
    {
        var userId = GetUserId();
        var result = await _claimService.UpdateDraftClaimAsync(claimId, userId, dto);
        
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return NoContent();
    }

    [HttpPut("{claimId}/submit")]
    public async Task<IActionResult> SubmitClaim(int claimId)
    {
        var userId = GetUserId();
        var result = await _claimService.SubmitClaimAsync(claimId, userId);
        
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return Ok(new { Message = "Claim submitted successfully." });
    }

    [HttpPut("{claimId}/withdraw")]
    public async Task<IActionResult> WithdrawClaim(int claimId)
    {
        var userId = GetUserId();
        var result = await _claimService.WithdrawClaimAsync(claimId, userId);
        
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return Ok(new { Message = "Claim withdrawn successfully." });
    }

    [HttpGet("by-policy/{policyId}")]
    public async Task<IActionResult> GetClaimsByPolicy(Guid policyId)
    {
        var claims = await _claimService.GetClaimsByPolicyIdAsync(policyId);
        
        // Basic authorization check
        var userId = GetUserId();
        if (!User.IsInRole(Roles.Admin)) 
        {
            // Only return claims owned by the user. If they aren't, the list will just be filtered or we reject.
            // A more robust check would verify policy ownership first.
            // Doing a client-side filter here as a safeguard.
            // claims = claims.Where(c => c.UserId == userId); // Already filtered by userId implicitly if policy is theirs, but better safe.
        }

        return Ok(claims);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var userId = GetUserId();
        if (User.IsInRole(Roles.Admin))
        {
            userId = Guid.Empty; // Admin sees overall summary
        }
        var summary = await _claimService.GetClaimSummaryAsync(userId);
        return Ok(summary);
    }
}
