using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSure.Admin.Application.Interfaces;

namespace SmartSure.Admin.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin/policies")]
public class AdminPoliciesController : ControllerBase
{
    private readonly IAdminPolicyService _policyService;

    public AdminPoliciesController(IAdminPolicyService policyService)
    {
        _policyService = policyService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPolicies(
        [FromQuery] string? searchTerm,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _policyService.GetPoliciesAsync(searchTerm, status, page, pageSize);
        return Ok(result);
    }
}
