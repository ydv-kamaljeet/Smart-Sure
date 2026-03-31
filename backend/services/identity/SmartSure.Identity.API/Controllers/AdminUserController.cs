using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSure.Identity.Application.DTOs;
using SmartSure.Identity.Application.Interfaces;
using SmartSure.Shared.Common.Constants;

namespace SmartSure.Identity.API.Controllers;

/// <summary>
/// Admin-only controller for managing users and their roles.
/// All endpoints require the caller to be authenticated with the "Admin" role.
/// Exposed at: api/auth/users
/// </summary>
[ApiController]
[Route("api/auth/users")]
[Authorize(Roles = Roles.Admin)]
public class AdminUserController : ControllerBase
{
    private readonly IAdminUserService _adminService;

    public AdminUserController(IAdminUserService adminService)
    {
        _adminService = adminService;
    }

    /// <summary>
    /// Returns a paginated list of all users.
    /// Optionally filter by role name and/or active status.
    /// Results are ordered newest-first.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10,
        [FromQuery] string? role = null,       // filter by role name, e.g. "Admin" or "Policyholder"
        [FromQuery] bool? isActive = null)     // filter by active/inactive users; null means all
    {
        var result = await _adminService.GetUsersAsync(page, pageSize, role, isActive);
        return Ok(result);
    }

    /// <summary>
    /// Assigns (replaces) the role of a user identified by userId.
    /// This overwrites any existing role — a user can only hold one role at a time.
    /// Publishes a UserRoleChangedEvent to RabbitMQ on success.
    /// </summary>
    [HttpPut("{userId}/roles")]
    public async Task<IActionResult> AssignRole(Guid userId, [FromBody] AssignRoleDto dto)
    {
        var result = await _adminService.AssignRoleAsync(userId, dto.RoleName);
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return Ok(new { Message = "Role assigned successfully." });
    }

    /// <summary>
    /// Revokes a specific named role from a user.
    /// Will fail if the user only has one role — every user must retain at least one role.
    /// </summary>
    [HttpDelete("{userId}/roles/{roleName}")]
    public async Task<IActionResult> RevokeRole(Guid userId, string roleName)
    {
        var result = await _adminService.RevokeRoleAsync(userId, roleName);
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return Ok(new { Message = "Role revoked successfully." });
    }
}
