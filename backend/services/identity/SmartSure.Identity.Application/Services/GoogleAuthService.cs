using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartSure.Identity.Application.DTOs;
using SmartSure.Identity.Application.Interfaces;
using SmartSure.Identity.Domain.Entities;
using SmartSure.Shared.Common.Models;
using SmartSure.Shared.Security.Jwt;

namespace SmartSure.Identity.Application.Services;

/// <summary>
/// Implements the Google OAuth 2.0 Authorization Code flow.
/// Flow: browser → /api/auth/google → Google consent → /api/auth/google/callback → JWT issued.
///
/// Steps:
///   1. GetGoogleConsentUrl() — builds the Google OAuth authorization URL and redirects the browser.
///   2. HandleCallbackAsync(code) — exchanges the one-time code for tokens, validates the ID token,
///      creates or links a user account, then issues a SmartSure JWT.
/// </summary>
public class GoogleAuthService : IGoogleAuthService
{
    private readonly GoogleAuthSettings _settings;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GoogleAuthService> _logger;

    public GoogleAuthService(
        IOptions<GoogleAuthSettings> settings,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        IJwtTokenGenerator jwtTokenGenerator,
        IHttpClientFactory httpClientFactory,
        ILogger<GoogleAuthService> logger)
    {
        _settings = settings.Value;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _jwtTokenGenerator = jwtTokenGenerator;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Builds the Google OAuth 2.0 consent screen URL with the required scopes.
    /// The browser will redirect to this URL; Google redirects back to RedirectUri after consent.
    /// Scopes requested: openid, email, profile.
    /// access_type=offline and prompt=consent force a refresh token to be issued.
    /// </summary>
    public string GetGoogleConsentUrl()
    {
        var scopes = "openid email profile";
        return $"https://accounts.google.com/o/oauth2/v2/auth?" +
               $"client_id={_settings.ClientId}" +
               $"&redirect_uri={Uri.EscapeDataString(_settings.RedirectUri)}" +
               $"&response_type=code" +
               $"&scope={Uri.EscapeDataString(scopes)}" +
               $"&access_type=offline" +
               $"&prompt=consent";
    }

    /// <summary>
    /// Handles the Google OAuth callback after the user grants consent.
    /// 1. Exchanges the one-time authorization code for a Google ID token (POST to token endpoint).
    /// 2. Validates the ID token via Google's tokeninfo endpoint to get verified user claims.
    /// 3. Creates a new user account OR links Google to an existing account if the email matches.
    /// 4. Issues a SmartSure RS256 JWT with the user's roles.
    /// </summary>
    public async Task<Result<LoginResponseDto>> HandleCallbackAsync(string authorizationCode)
    {
        try
        {
            // Exchange authorization code for tokens
            var client = _httpClientFactory.CreateClient();
            var tokenResponse = await client.PostAsync("https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["code"] = authorizationCode,
                    ["client_id"] = _settings.ClientId,
                    ["client_secret"] = _settings.ClientSecret,
                    ["redirect_uri"] = _settings.RedirectUri,
                    ["grant_type"] = "authorization_code"
                }));

            if (!tokenResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Google token exchange failed: {Status}", tokenResponse.StatusCode);
                return Result<LoginResponseDto>.Failure("Failed to exchange Google authorization code.");
            }

            var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<GoogleTokenResponse>();
            if (tokenResult == null || string.IsNullOrEmpty(tokenResult.IdToken))
                return Result<LoginResponseDto>.Failure("Invalid Google token response.");

            // Validate ID token using Google's tokeninfo endpoint — ensures it was issued by Google for our client
            var validationResponse = await client.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={tokenResult.IdToken}");
            if (!validationResponse.IsSuccessStatusCode)
                return Result<LoginResponseDto>.Failure("Google ID token validation failed.");

            var googleUser = await validationResponse.Content.ReadFromJsonAsync<GoogleUserInfo>();
            if (googleUser == null || string.IsNullOrEmpty(googleUser.Email))
                return Result<LoginResponseDto>.Failure("Failed to retrieve Google user info.");

            // Find existing user by email OR create a new account for first-time Google sign-in
            var user = await _userRepository.GetByEmailAsync(googleUser.Email);
            if (user == null)
            {
                // New user — create account with default Policyholder role; email is Google-verified so skip verification
                user = new User
                {
                    UserId = Guid.NewGuid(),
                    Email = googleUser.Email,
                    FullName = googleUser.Name ?? googleUser.Email,
                    IsEmailVerified = true, // Google-verified email — no additional verification needed
                    IsActive = true
                };

                var defaultRole = await _roleRepository.GetByNameAsync("Policyholder");
                if (defaultRole != null)
                    user.UserRoles.Add(new UserRole { RoleId = defaultRole.RoleId });

                // Link the Google provider identity to the new account
                user.ExternalLogins.Add(new ExternalLogin
                {
                    Provider = "Google",
                    ProviderKey = googleUser.Sub // Sub is the stable Google user ID
                });

                await _userRepository.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                // Existing user — link Google account if not already linked (first time using Google SSO)
                if (!user.ExternalLogins.Any(e => e.Provider == "Google" && e.ProviderKey == googleUser.Sub))
                {
                    user.ExternalLogins.Add(new ExternalLogin
                    {
                        Provider = "Google",
                        ProviderKey = googleUser.Sub
                    });
                    await _userRepository.UpdateAsync(user);
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            // Issue a SmartSure JWT with the same claims as a normal email/password login
            var roles = user.UserRoles.Select(ur => ur.Role!.Name).ToList();
            var jwt = _jwtTokenGenerator.GenerateToken(user.UserId, user.Email, roles);
            var refreshToken = _jwtTokenGenerator.GenerateToken(user.UserId, user.Email, roles, "refresh", 60 * 24 * 7);

            return Result<LoginResponseDto>.Success(
                new LoginResponseDto(jwt, refreshToken, user.Email, user.FullName, roles.ToArray()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google OAuth callback failed");
            return Result<LoginResponseDto>.Failure("An error occurred during Google authentication.");
        }
    }

    // Private models for deserializing Google API responses

    /// <summary>Response from the Google OAuth token endpoint.</summary>
    private class GoogleTokenResponse
    {
        [JsonPropertyName("id_token")]
        public string IdToken { get; set; } = string.Empty;

        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
    }

    /// <summary>User claims extracted from the validated Google ID token via tokeninfo.</summary>
    private class GoogleUserInfo
    {
        [JsonPropertyName("sub")]
        public string Sub { get; set; } = string.Empty; // stable Google user identifier

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
