using BCrypt.Net;
using Microsoft.Extensions.Configuration;
using SmartSure.Identity.Application.DTOs;
using SmartSure.Identity.Application.Interfaces;
using SmartSure.Identity.Domain.Entities;
using SmartSure.Shared.Common.Models;
using SmartSure.Shared.Security.Jwt;
using SmartSure.Shared.Contracts.Events;
using MassTransit;
using System.Text.Json;

namespace SmartSure.Identity.Application.Services;

// AuthService handles all authentication and user account operations.
// It is the core business logic layer — controllers call this, this calls repositories.
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IEmailService _emailService;
    private readonly IOtpService _otpService;
    private readonly ITokenBlacklistService _tokenBlacklist;
    private readonly IPublishEndpoint _publishEndpoint; // RabbitMQ publisher for broadcasting events to other services
    private readonly IConfiguration _configuration;

    public AuthService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        IJwtTokenGenerator jwtTokenGenerator,
        IEmailService emailService,
        IOtpService otpService,
        ITokenBlacklistService tokenBlacklist,
        IPublishEndpoint publishEndpoint,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _jwtTokenGenerator = jwtTokenGenerator;
        _emailService = emailService;
        _otpService = otpService;
        _tokenBlacklist = tokenBlacklist;
        _publishEndpoint = publishEndpoint;
        _configuration = configuration;
    }

    // Registers a new user: checks for duplicate email, hashes password with BCrypt,
    // assigns default "Policyholder" role, saves to DB, publishes event to RabbitMQ, sends verification email.
    public async Task<Result> RegisterAsync(RegisterDto dto)
    {
        var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            return Result.Failure("Email is already registered.");
        }

        // Generate a unique token for email verification link (valid for 24 hours)
        var verificationToken = Guid.NewGuid().ToString();

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = dto.Email,
            FullName = dto.FullName,
            IsEmailVerified = false, // user cannot login until they verify their email
            VerificationToken = verificationToken,
            VerificationTokenExpiry = DateTime.UtcNow.AddHours(24)
        };

        // Every new user gets the "Policyholder" role by default
        var defaultRole = await _roleRepository.GetByNameAsync("Policyholder");
        if (defaultRole == null)
        {
            return Result.Failure("Default role 'Policyholder' not found. Please contact support.");
        }

        user.UserRoles.Add(new UserRole { RoleId = defaultRole.RoleId });

        // Password is BCrypt hashed before storing — never stored as plain text
        user.Passwords.Add(new Password
        {
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        });

        try
        {
            await _userRepository.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            // Throw the custom exception and wrap the inbuilt exception as the InnerException
            // This ensures the GlobalExceptionHandler/Serilog will automatically record the actual DB stack trace 
            // without requiring per-service ILogger injection.
            throw new SmartSure.Shared.Common.Exceptions.ConflictException("An error occurred trying to register the user. The email may already be in use.", ex);
        }

        try
        {
            await _publishEndpoint.Publish(new UserRegisteredEvent(
            user.UserId, user.FullName, user.Email, defaultRole.Name, DateTime.UtcNow));
        }
        catch { /* swallow — non-critical */ }

        // Send verification email with a clickable link containing the token
        var verifyLink = $"{_configuration["AppSettings:BaseUrl"]}/api/auth/verify-email?token={verificationToken}";
        await _emailService.SendEmailAsync(dto.Email, "Verify Your SmartSure Account",
            $"Welcome to SmartSure! Click here to verify your email: {verifyLink}");

        return Result.Success();
    }

    // Validates credentials, checks email verification status, verifies BCrypt hash,
    // generates RS256 JWT token, and publishes a login event to RabbitMQ.
    public async Task<Result<LoginResponseDto>> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);

        // Return same generic error for both "user not found" and "inactive" — avoids leaking which emails exist
        if (user == null || !user.IsActive)
            return Result<LoginResponseDto>.Failure("Invalid credentials or user inactive.");

        // Block login if email is not verified yet
        if (!user.IsEmailVerified)
            return Result<LoginResponseDto>.Failure("Please verify your email first.");

        // Get the most recently changed password and verify the BCrypt hash
        var currentPassword = user.Passwords.OrderByDescending(p => p.LastChangedAt).FirstOrDefault();
        if (currentPassword == null || !BCrypt.Net.BCrypt.Verify(dto.Password, currentPassword.PasswordHash))
            return Result<LoginResponseDto>.Failure("Invalid credentials.");

        var roles = user.UserRoles.Select(ur => ur.Role!.Name).ToList();

        // Generate a signed RS256 JWT with userId, email, and roles packed as claims
        var accessToken = _jwtTokenGenerator.GenerateToken(user.UserId, user.Email, roles);

        // Generate a long-lived refresh token (7 days) scoped with purpose="refresh"
        // Stored in browser localStorage — used to silently get a new access token when it expires
        var refreshToken = _jwtTokenGenerator.GenerateToken(user.UserId, user.Email, roles, "refresh", 60 * 24 * 7);

        // Fire-and-forget — RabbitMQ unavailability must never block login
        _ = Task.Run(async () =>
        {
            try
            {
                await _publishEndpoint.Publish(new UserLoggedInEvent(user.UserId, user.Email, DateTime.UtcNow));
            }
            catch { /* swallow — non-critical */ }
        });

        return Result<LoginResponseDto>.Success(new LoginResponseDto(accessToken, refreshToken, user.Email, user.FullName, roles.ToArray()));
    }

    // Logout blacklists the current JWT token in memory cache so it cannot be reused
    // even if it hasn't expired yet. TTL matches the token's remaining lifetime (1 hour max).
    public async Task<Result> LogoutAsync(Guid userId, string token)
    {
        await _tokenBlacklist.BlacklistTokenAsync(token, TimeSpan.FromHours(1));
        return Result.Success();
    }

    // Validates the refresh token signature, expiry, and purpose claim.
    // If valid, issues a new short-lived access token (1 hour).
    // The refresh token itself is NOT rotated — same refresh token can be reused until it expires (7 days).
    public async Task<Result<string>> RefreshAsync(string refreshToken)
    {
        // Check if the refresh token has been blacklisted (e.g. after logout)
        if (await _tokenBlacklist.IsBlacklistedAsync(refreshToken))
            return Result<string>.Failure("Refresh token has been revoked.");

        // Validate the JWT signature and extract claims using the token generator
        var principal = _jwtTokenGenerator.ValidateToken(refreshToken);
        if (principal == null)
            return Result<string>.Failure("Invalid or expired refresh token.");

        // Ensure this token was issued specifically as a refresh token
        var purpose = principal.FindFirst("purpose")?.Value;
        if (purpose != "refresh")
            return Result<string>.Failure("Invalid token purpose.");

        // Extract userId from the sub claim and load the user
        var userIdStr = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            return Result<string>.Failure("Invalid token subject.");

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || !user.IsActive)
            return Result<string>.Failure("User not found or inactive.");

        // Issue a fresh access token with current roles
        var roles = user.UserRoles.Select(ur => ur.Role!.Name).ToList();
        var newAccessToken = _jwtTokenGenerator.GenerateToken(user.UserId, user.Email, roles);

        return Result<string>.Success(newAccessToken);
    }

    // Verifies the email verification token from the registration link.
    // Marks the user as verified and clears the token so it cannot be reused.
    public async Task<Result> VerifyEmailAsync(string token)
    {
        // Query checks both token match AND expiry — expired tokens return null
        var user = await _userRepository.GetByVerificationTokenAsync(token);
        if (user == null)
            return Result.Failure("Invalid or expired verification token.");

        user.IsEmailVerified = true;
        user.VerificationToken = null;       // clear token so it cannot be reused
        user.VerificationTokenExpiry = null;

        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    // Generates a fresh verification token and resends the verification email.
    // Used when the original 24-hour token has expired.
    public async Task<Result> ResendVerificationEmailAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null || user.IsEmailVerified) return Result.Failure("User not found or already verified.");

        user.VerificationToken = Guid.NewGuid().ToString();
        user.VerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        
        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var verifyLink = $"{_configuration["AppSettings:BaseUrl"]}/api/auth/verify-email?token={user.VerificationToken}";
        await _emailService.SendEmailAsync(user.Email, "Verify Your SmartSure Account",
            $"Click here to verify your email: {verifyLink}");

        return Result.Success();
    }

    // Step 1 of password reset: generates a 6-digit OTP, BCrypt-hashes it, stores it in DB,
    // and emails the plain OTP to the user. Always returns success to prevent user enumeration.
    public async Task<Result> ForgotPasswordAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);

        // Return success even if email doesn't exist — prevents attackers from discovering registered emails
        if (user == null) return Result.Success();

        var otpResult = await _otpService.GenerateOtpAsync(email);
        if (!otpResult.IsSuccess) return Result.Failure(otpResult.ErrorMessage!);

        await _emailService.SendEmailAsync(email, "Password Reset OTP",
            $"Your OTP code is {otpResult.Data}. It expires in 10 minutes.");
        return Result.Success();
    }

    // Step 2 of password reset: validates the OTP against the stored BCrypt hash.
    // On success, issues a short-lived (15 min) JWT with purpose="password-reset" claim.
    // This reset token is required to call ResetPasswordAsync.
    public async Task<Result<string>> VerifyOtpAsync(VerifyOtpDto dto)
    {
        var validResult = await _otpService.ValidateOtpAsync(dto.Email, dto.OtpCode);
        if (!validResult.IsSuccess) return Result<string>.Failure(validResult.ErrorMessage!);

        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null) return Result<string>.Failure("User not found.");

        // Issue a scoped reset token — expires in 15 minutes, purpose claim prevents misuse as a login token
        var resetToken = _jwtTokenGenerator.GenerateToken(user.UserId, user.Email, new List<string>(), "password-reset", 15);
        return Result<string>.Success(resetToken);
    }

    // Step 3 of password reset: BCrypt-hashes the new password, updates the Passwords table,
    // then blacklists the reset token so it cannot be reused.
    public async Task<Result> ResetPasswordAsync(ResetPasswordDto dto, string resetToken)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null) return Result.Failure("Invalid request.");

            // Always update the most recently changed password record
            var currentPassword = user.Passwords.OrderByDescending(p => p.LastChangedAt).FirstOrDefault();
            if (currentPassword == null) return Result.Failure("No password record found.");

            currentPassword.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            currentPassword.LastChangedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Blacklist the reset token immediately after use — one-time use only
            await _tokenBlacklist.BlacklistTokenAsync(resetToken, TimeSpan.FromMinutes(15));

            return Result.Success();
        }

    // Returns the current user's profile by resolving userId from the JWT sub claim.
    public async Task<Result<UserProfileDto>> GetProfileAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return Result<UserProfileDto>.Failure("User not found.");

        return Result<UserProfileDto>.Success(new UserProfileDto(user.UserId, user.FullName, user.Email, user.Phone, user.Address, user.IsEmailVerified));
    }

    // Updates the user's profile fields (name, phone, address). Email cannot be changed here.
    public async Task<Result> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return Result.Failure("User not found.");

        user.FullName = dto.FullName;
        user.Phone = dto.Phone;
        user.Address = dto.Address;
        user.UpdatedAt = DateTime.UtcNow;
        
        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    // Changes the user's password: verifies the current password first, then BCrypt-hashes the new one.
    public async Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return Result.Failure("User not found.");

        var currentPassword = user.Passwords.OrderByDescending(p => p.LastChangedAt).FirstOrDefault();
        if (currentPassword == null || !BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, currentPassword.PasswordHash))
            return Result.Failure("Invalid current password.");

        currentPassword.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        currentPassword.LastChangedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<LoginResponseDto>> HandleGoogleCallbackAsync(string authorizationCode)
    {
        // Google OAuth is fully handled by GoogleAuthService — delegated to keep this class focused
        throw new NotImplementedException("Google OAuth handled by IGoogleAuthService.");
    }
}
