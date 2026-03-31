using SmartSure.Identity.Application.DTOs;
using SmartSure.Shared.Common.Models;

namespace SmartSure.Identity.Application.Interfaces;

public interface IAdminUserService
{
    Task<PagedResult<UserProfileDto>> GetUsersAsync(int page, int pageSize, string? role, bool? isActive);
    Task<Result> AssignRoleAsync(Guid userId, string roleName);
    Task<Result> RevokeRoleAsync(Guid userId, string roleName);
}
