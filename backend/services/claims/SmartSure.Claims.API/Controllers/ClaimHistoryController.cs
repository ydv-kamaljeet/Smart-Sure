using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSure.Claims.Application.Interfaces;

namespace SmartSure.Claims.API.Controllers;

[ApiController]
[Route("api/claims/{claimId}/history")]
[Authorize]
public class ClaimHistoryController : ControllerBase
{
    private readonly IClaimManagementService _claimService;

    public ClaimHistoryController(IClaimManagementService claimService)
    {
        _claimService = claimService;
    }

    [HttpGet]
    public async Task<IActionResult> GetHistory(int claimId)
    {
        var history = await _claimService.GetClaimHistoryAsync(claimId);
        return Ok(history);
    }
}
