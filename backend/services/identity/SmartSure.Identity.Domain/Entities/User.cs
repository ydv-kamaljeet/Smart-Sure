namespace SmartSure.Identity.Domain.Entities;

/// <summary>
/// Core identity entity representing a registered user in the system.
/// Passwords are stored in a separate Passwords table (not on this entity) for auditability.
/// Roles are assigned via the UserRoles join table.
/// External OAuth logins (e.g. Google) are tracked in ExternalLogins.
/// </summary>
public class User
{
    /// <summary>Primary key — generated as a new GUID on creation.</summary>
    public Guid UserId { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>Optional contact phone number.</summary>
    public string? Phone { get; set; }

    /// <summary>Optional mailing/billing address.</summary>
    public string? Address { get; set; }

    /// <summary>
    /// Soft-delete / suspension flag. Inactive users cannot log in.
    /// Defaults to true (active) on creation.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Set to true after the user clicks the verification link in their registration email.
    /// Unverified users are blocked from logging in.
    /// </summary>
    public bool IsEmailVerified { get; set; } = false;

    /// <summary>
    /// GUID token embedded in the email verification link.
    /// Cleared to null after the user verifies, preventing reuse.
    /// </summary>
    public string? VerificationToken { get; set; }

    /// <summary>Expiry for the verification token — tokens older than 24 hours are rejected.</summary>
    public DateTime? VerificationTokenExpiry { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Soft-delete timestamp. Not currently used but reserved for future use.</summary>
    public DateTime? DeletedAt { get; set; }

    // Navigation properties — EF Core loads these via Include() calls in the repository
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<ExternalLogin> ExternalLogins { get; set; } = new List<ExternalLogin>();
    public ICollection<Password> Passwords { get; set; } = new List<Password>();
}
