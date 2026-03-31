using System.ComponentModel.DataAnnotations;

namespace SmartSure.Identity.Application.DTOs;

/// <summary>
/// Payload for registering a new user account.
/// Password must be at least 6 characters; the service hashes it with BCrypt before storage.
/// </summary>
public record RegisterDto(
    [Required] [EmailAddress] string Email, 
    [Required] string FullName, 
    [Required] [MinLength(6)] string Password);

/// <summary>
/// Payload for standard email/password login.
/// </summary>
public record LoginDto(
    [Required] [EmailAddress] string Email, 
    [Required] string Password);

/// <summary>
/// Returned after a successful login or Google OAuth callback.
/// Contains the RS256-signed JWT access token, a long-lived refresh token, and basic user info.
/// </summary>
public record LoginResponseDto(string AccessToken, string RefreshToken, string Email, string FullName, string[] Roles);

/// <summary>
/// Payload for refreshing an expired access token using a valid refresh token.
/// </summary>
public record RefreshTokenDto([Required] string RefreshToken);

/// <summary>
/// Payload for Step 2 of the forgot-password flow — submitting the 6-digit OTP
/// that was emailed to the user in Step 1.
/// </summary>
public record VerifyOtpDto(
    [Required] [EmailAddress] string Email, 
    [Required] string OtpCode);

/// <summary>
/// Payload for Step 3 of the forgot-password flow — setting a new password.
/// Requires a valid purpose-scoped reset JWT in the Authorization header.
/// </summary>
public record ResetPasswordDto(
    [Required] [EmailAddress] string Email, 
    [Required] [MinLength(6)] string NewPassword);

/// <summary>
/// Payload for updating the authenticated user's own profile fields.
/// Email cannot be changed via this endpoint.
/// </summary>
public record UpdateProfileDto(
    [Required] string FullName, 
    string? Phone, 
    string? Address);

/// <summary>
/// Read-only projection of a user's public profile fields.
/// Returned by GET /api/auth/me and the admin user list.
/// </summary>
public record UserProfileDto(Guid UserId, string FullName, string Email, string? Phone, string? Address, bool IsEmailVerified);

/// <summary>
/// Payload for changing the authenticated user's own password.
/// CurrentPassword is verified against the stored BCrypt hash before updating.
/// </summary>
public record ChangePasswordDto(
    [Required] string CurrentPassword, 
    [Required] [MinLength(6)] string NewPassword);

/// <summary>
/// Payload for the admin endpoint that assigns a role to a user.
/// </summary>
public record AssignRoleDto(
    [Required] string RoleName);
