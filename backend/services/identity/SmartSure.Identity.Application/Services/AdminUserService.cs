using MassTransit;
using SmartSure.Identity.Application.DTOs;
using SmartSure.Identity.Application.Interfaces;
using SmartSure.Shared.Common.Models;
using SmartSure.Shared.Contracts.Events;

namespace SmartSure.Identity.Application.Services;

/// <summary>
/// Handles admin-only user management operations: listing users and managing their roles.
/// Publishes a UserRoleChangedEvent to RabbitMQ whenever a role is assigned,
/// so other services (e.g. Policy, Claims) can react to privilege changes.
/// </summary>
public class AdminUserService : IAdminUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBus _bus; // MassTransit bus for publishing domain events to RabbitMQ

    public AdminUserService(IUserRepository userRepository, IRoleRepository roleRepository, IUnitOfWork unitOfWork, IBus bus)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _bus = bus;
    }

    /// <summary>
    /// Returns a paginated, filterable list of users projected to UserProfileDto.
    /// Filtering by role and/or active status is optional — passing null skips that filter.
    /// </summary>
    public async Task<PagedResult<UserProfileDto>> GetUsersAsync(int page, int pageSize, string? role, bool? isActive)
    {
        var result = await _userRepository.GetPagedAsync(page, pageSize, role, isActive);
        
        // Project domain entities to DTOs — never expose raw User entities to the API layer
        var dtos = result.Items.Select(u => new UserProfileDto(
            u.UserId, u.FullName, u.Email, u.Phone, u.Address, u.IsEmailVerified
        ));

        return new PagedResult<UserProfileDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = result.TotalCount
        };
    }

    /// <summary>
    /// Replaces the user's existing role with the given role.
    /// Only one role per user is supported — this is an atomic replace, not an add.
    /// Publishes UserRoleChangedEvent to notify downstream services of the change.
    /// </summary>
    public async Task<Result> AssignRoleAsync(Guid userId, string roleName)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return Result.Failure("User not found.");

        var newRole = await _roleRepository.GetByNameAsync(roleName);
        if (newRole == null) return Result.Failure("Role does not exist.");

        // Replace all existing roles with the new one (deletes old rows, inserts new)
        await _userRepository.ReplaceRoleAsync(userId, newRole.RoleId);
        await _unitOfWork.SaveChangesAsync();

        // Notify other services that this user's role has changed
        await _bus.Publish(new UserRoleChangedEvent(userId, roleName, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Removes a specific named role from a user.
    /// Fails if the user would be left with no roles at all — every user must retain at least one.
    /// </summary>
    public async Task<Result> RevokeRoleAsync(Guid userId, string roleName)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return Result.Failure("User not found.");

        var roleMapping = user.UserRoles.FirstOrDefault(ur => ur.Role!.Name == roleName);
        if (roleMapping != null)
        {
            // Guard: prevent removing the last role — a user with zero roles would be unable to authenticate
            if (user.UserRoles.Count <= 1)
                return Result.Failure("Cannot revoke the last role. Every user must have at least one role.");

            user.UserRoles.Remove(roleMapping);
            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
        }

        return Result.Success();
    }
}
