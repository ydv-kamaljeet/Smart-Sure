using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSure.Identity.Application.DTOs;
using SmartSure.Identity.Application.Interfaces;
using System.Security.Claims;

namespace SmartSure.Identity.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IGoogleAuthService _googleAuthService;

    public AuthController(IAuthService authService, IGoogleAuthService googleAuthService)
    {
        _authService = authService;
        _googleAuthService = googleAuthService;
    }

    /// <summary>
    /// Register a new customer — BCrypt hash, sets IsEmailVerified=false, sends verification link.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return Ok(new { Message = "Registration successful. Please check your email to verify." });
    }

    /// <summary>
    /// Authenticate; checks IsEmailVerified before issuing RS256 JWT (1 h expiry).
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        if (!result.IsSuccess) return Unauthorized(new { result.ErrorMessage });
        return Ok(result.Data);
    }

    /// <summary>
    /// Invalidate token — blacklist entry written to cache.
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        var result = await _authService.LogoutAsync(userId, token);
        return Ok(new { Message = "Logged out successfully." });
    }

    /// <summary>
    /// Exchange a valid refresh token for a new access token.
    /// Refresh token is validated for signature, expiry, and purpose="refresh" claim.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
    {
        var result = await _authService.RefreshAsync(dto.RefreshToken);
        if (!result.IsSuccess) return Unauthorized(new { result.ErrorMessage });
        return Ok(new { AccessToken = result.Data });
    }

    /// <summary>
    /// Verify email using GUID token from registration link (24 h expiry).
    /// </summary>
    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        var result = await _authService.VerifyEmailAsync(token);
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return Ok(new { Message = "Email verified successfully." });
    }

    /// <summary>
    /// Generate fresh verification token and resend email to user's inbox.
    /// </summary>
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromQuery] string email)
    {
        var result = await _authService.ResendVerificationEmailAsync(email);
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return Ok(new { Message = "Verification email resent." });
    }

    /// <summary>
    /// Step 1 — generate cryptographically random 6-digit OTP, BCrypt-hash, store with 10-min expiry, email to user.
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromQuery] string email)
    {
        var result = await _authService.ForgotPasswordAsync(email);
        return Ok(new { Message = "If that email exists, an OTP has been sent." });
    }

    /// <summary>
    /// Step 2 — validate OTP against stored BCrypt hash; issue short-lived (15 min) password-reset JWT on success; lock after 3 failures.
    /// </summary>
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
    {
        var result = await _authService.VerifyOtpAsync(dto);
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return Ok(new { ResetToken = result.Data });
    }

    /// <summary>
    /// Step 3 — verify reset JWT purpose claim, BCrypt-hash new password, update Passwords table, invalidate token.
    /// </summary>
    [Authorize]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        if (User.FindFirstValue("purpose") != "password-reset")
            return Forbid();

        var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        var result = await _authService.ResetPasswordAsync(dto, token);
        
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return Ok(new { Message = "Password reset successfully." });
    }

    /// <summary>
    /// Return current user's profile resolved from JWT sub claim.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _authService.GetProfileAsync(userId);
        if (!result.IsSuccess) return NotFound(new { result.ErrorMessage });
        return Ok(result.Data);
    }

    /// <summary>
    /// Update full name, phone, address — validated DTO.
    /// </summary>
    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _authService.UpdateProfileAsync(userId, dto);
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return Ok(new { Message = "Profile updated." });
    }

    /// <summary>
    /// Change password: verify old hash, BCrypt-hash new password, update Passwords table.
    /// </summary>
    [Authorize]
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _authService.ChangePasswordAsync(userId, dto);
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return Ok(new { Message = "Password changed successfully." });
    }

    /// <summary>
    /// Redirect browser to Google OAuth 2.0 consent screen.
    /// </summary>
    [HttpGet("google")]
    public IActionResult GoogleLogin()
    {
        var url = _googleAuthService.GetGoogleConsentUrl();
        return Redirect(url);
    }

    /// <summary>
    /// Exchange Google auth code for ID token; create/link account, redirect to frontend with JWT.
    /// </summary>
    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback([FromQuery] string code)
    {
        var result = await _googleAuthService.HandleCallbackAsync(code);
        if (!result.IsSuccess)
            return Redirect($"http://localhost:4200/login?google_error={Uri.EscapeDataString(result.ErrorMessage ?? "Authentication failed")}");

        var data = result.Data!;
        // Use JSON + base64 to avoid any token character encoding issues
        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            accessToken = data.AccessToken,
            email = data.Email,
            fullName = data.FullName,
            roles = data.Roles
        });
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload));
        return Redirect($"http://localhost:4200/auth/google/callback?data={Uri.EscapeDataString(encoded)}");
    }
}
