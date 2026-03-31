using Microsoft.EntityFrameworkCore;
using SmartSure.Identity.Application.Interfaces;
using SmartSure.Identity.Domain.Entities;
using SmartSure.Identity.Infrastructure.Data;
using SmartSure.Shared.Common.Models;

namespace SmartSure.Identity.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of IUserRepository.
/// All read queries eagerly load UserRoles (with Role) and Passwords
/// so services never encounter lazy-loading issues or N+1 queries.
/// Write operations (Add/Update) only stage changes — call IUnitOfWork.SaveChangesAsync to commit.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _context;

    public UserRepository(IdentityDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Fetches a user by primary key, including their roles and password records.
    /// Returns null if no user with the given ID exists.
    /// </summary>
    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role) // load Role navigation so role.Name is accessible
            .Include(u => u.Passwords)
            .FirstOrDefaultAsync(u => u.UserId == id);
    }

    /// <summary>
    /// Fetches a user by email address, including their roles and password records.
    /// Used by login and registration duplicate-check flows.
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Include(u => u.Passwords)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    /// <summary>
    /// Fetches a user whose VerificationToken matches AND whose VerificationTokenExpiry
    /// is still in the future. Returns null for expired or invalid tokens.
    /// </summary>
    public async Task<User?> GetByVerificationTokenAsync(string token)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.VerificationToken == token && u.VerificationTokenExpiry > DateTime.UtcNow);
    }

    /// <summary>Stages the new User for insertion (persisted on next SaveChangesAsync).</summary>
    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    /// <summary>Marks the existing User entity as modified (persisted on next SaveChangesAsync).</summary>
    public Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Upserts the user's role: if a UserRole row exists for this user, its RoleId is updated;
    /// otherwise a new UserRole row is inserted. Used by AdminUserService.AssignRoleAsync.
    /// </summary>
    public async Task ReplaceRoleAsync(Guid userId, int newRoleId)
    {
        var existing = await _context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == userId);
        if (existing != null)
        {
            // Update in place — avoids deleting and re-inserting the row
            existing.RoleId = newRoleId;
        }
        else
        {
            // Fallback: user has no role row yet, insert one
            _context.UserRoles.Add(new UserRole { UserId = userId, RoleId = newRoleId });
        }
    }

    /// <summary>
    /// Returns a paginated list of users, optionally filtered by role name and/or active status.
    /// Results are ordered by CreatedAt descending (newest users first).
    /// Uses SplitQuery behavior (configured in Program.cs) to avoid Cartesian explosion on multi-Include queries.
    /// </summary>
    public async Task<PagedResult<User>> GetPagedAsync(int page, int pageSize, string? role, bool? isActive)
    {
        var query = _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .AsQueryable();

        // Apply optional filters
        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        if (!string.IsNullOrEmpty(role))
        {
            query = query.Where(u => u.UserRoles.Any(ur => ur.Role!.Name == role));
        }

        // Count total matching records for pagination metadata
        var total = await query.CountAsync();

        // Apply ordering and pagination
        var items = await query.OrderByDescending(u => u.CreatedAt)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();

        return new PagedResult<User>
        {
            TotalCount = total,
            Items = items,
            Page = page,
            PageSize = pageSize
        };
    }
}
