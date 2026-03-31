using SmartSure.Identity.Domain.Entities;
using SmartSure.Shared.Common.Models;

namespace SmartSure.Identity.Application.Interfaces;

/// <summary>
/// Repository contract for User persistence operations.
/// Consumed by AuthService and AdminUserService via DI.
/// Implementation: UserRepository (Infrastructure layer).
/// </summary>
public interface IUserRepository
{
    /// <summary>Fetches a user with their roles and passwords by primary key.</summary>
    Task<User?> GetByIdAsync(Guid id);

    /// <summary>Fetches a user with their roles and passwords by email address.</summary>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Fetches a user whose VerificationToken matches the given token AND whose
    /// VerificationTokenExpiry is in the future. Returns null if expired or not found.
    /// </summary>
    Task<User?> GetByVerificationTokenAsync(string token);

    /// <summary>Adds a new User entity to the change tracker (SaveChanges required to persist).</summary>
    Task AddAsync(User user);

    /// <summary>Marks an existing User entity as modified (SaveChanges required to persist).</summary>
    Task UpdateAsync(User user);

    /// <summary>Returns a filtered, paginated list of users ordered newest-first.</summary>
    Task<PagedResult<User>> GetPagedAsync(int page, int pageSize, string? role, bool? isActive);

    /// <summary>
    /// Replaces (or inserts) a user's single role row with newRoleId.
    /// Works as an upsert — updates the existing row if present, inserts if missing.
    /// </summary>
    Task ReplaceRoleAsync(Guid userId, int newRoleId);
}

/// <summary>
/// Repository contract for Role lookups.
/// Roles are seeded at startup; no write operations are needed at runtime.
/// </summary>
public interface IRoleRepository
{
    /// <summary>Returns the Role entity matching the given name, or null if it does not exist.</summary>
    Task<Role?> GetByNameAsync(string name);
}

/// <summary>
/// Repository contract for OTP record persistence.
/// One OTP record per email is enforced by the service layer (old records are deleted before inserting new ones).
/// </summary>
public interface IOtpRepository
{
    /// <summary>Returns the OTP record for the given email, or null if none exists.</summary>
    Task<OtpRecord?> GetByEmailAsync(string email);

    /// <summary>Adds a new OtpRecord to the change tracker (SaveChanges required to persist).</summary>
    Task AddAsync(OtpRecord record);

    /// <summary>Marks an existing OtpRecord as modified (SaveChanges required to persist).</summary>
    Task UpdateAsync(OtpRecord record);

    /// <summary>Removes an OtpRecord from the change tracker (SaveChanges required to persist).</summary>
    Task DeleteAsync(OtpRecord record);
}

/// <summary>
/// Unit-of-Work abstraction that wraps DbContext.SaveChangesAsync.
/// All repository write operations (Add/Update/Delete) are tracked in-memory;
/// call SaveChangesAsync to commit them as a single atomic DB transaction.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
