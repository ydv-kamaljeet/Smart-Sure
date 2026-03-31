using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSure.Admin.Application.DTOs;
using SmartSure.Admin.Application.Interfaces;

namespace SmartSure.Admin.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUsersService _usersService;

    public AdminUsersController(IAdminUsersService usersService)
    {
        _usersService = usersService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] string? searchTerm, [FromQuery] string? role, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _usersService.GetUsersAsync(searchTerm, role, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUserById(Guid userId)
    {
        var result = await _usersService.GetUserByIdAsync(userId);
        return result != null ? Ok(result) : NotFound();
    }

    [HttpDelete("{userId}")]
    public async Task<IActionResult> SoftDeleteUser(Guid userId)
    {
        var success = await _usersService.SoftDeleteUserAsync(userId);
        return success ? NoContent() : NotFound();
    }
}
