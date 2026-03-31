using SmartSure.Identity.Application.DTOs;
using SmartSure.Shared.Common.Models;

namespace SmartSure.Identity.Application.Interfaces;

/// <summary>
/// Contract for the core authentication and account-management service.
/// Implementation: AuthService (Application layer).
/// </summary>
public interface IAuthService
{
    /// <summary>Registers a new user, hashes their password, assigns the default role, and sends a verification email.</summary>
    Task<Result> RegisterAsync(RegisterDto dto);

    /// <summary>Validates credentials and returns a signed RS256 JWT on success.</summary>
    Task<Result<LoginResponseDto>> LoginAsync(LoginDto dto);

    /// <summary>Validates a refresh token and issues a new access token.</summary>
    Task<Result<string>> RefreshAsync(string refreshToken);

    /// <summary>Blacklists the current JWT token so it cannot be used after logout.</summary>
    Task<Result> LogoutAsync(Guid userId, string token);

    /// <summary>Marks the user's email as verified using the GUID token from the registration link.</summary>
    Task<Result> VerifyEmailAsync(string token);

    /// <summary>Generates a new verification token and resends the verification email.</summary>
    Task<Result> ResendVerificationEmailAsync(string email);

    /// <summary>Step 1 — generates and emails a 6-digit OTP for the forgot-password flow.</summary>
    Task<Result> ForgotPasswordAsync(string email);

    /// <summary>Step 2 — validates the OTP and returns a short-lived purpose-scoped reset JWT.</summary>
    Task<Result<string>> VerifyOtpAsync(VerifyOtpDto dto); // Returns a purpose-scoped reset JWT

    /// <summary>Step 3 — hashes and saves the new password; invalidates the reset token.</summary>
    Task<Result> ResetPasswordAsync(ResetPasswordDto dto, string resetToken);

    /// <summary>Returns the authenticated user's profile resolved from their JWT userId claim.</summary>
    Task<Result<UserProfileDto>> GetProfileAsync(Guid userId);

    /// <summary>Updates the user's FullName, Phone, and Address.</summary>
    Task<Result> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);

    /// <summary>Verifies the current password, then replaces it with a BCrypt hash of the new password.</summary>
    Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);

    /// <summary>
    /// Not used directly — Google OAuth is fully delegated to IGoogleAuthService.
    /// Kept on the interface for completeness.
    /// </summary>
    Task<Result<LoginResponseDto>> HandleGoogleCallbackAsync(string authorizationCode);
}
