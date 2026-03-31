using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace SmartSure.Shared.Security.Jwt;

// This class is responsible for generating JWT tokens using RSA asymmetric encryption (RS256).
// It lives in the Shared library so all services can reference the interface, but only the Identity service uses it to CREATE tokens.
public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _jwtSettings;
    private readonly RSA _rsa; // RSA private key — injected once at startup, reused for all token generation

    public JwtTokenGenerator(IOptions<JwtSettings> jwtOptions, RSA rsaKey)
    {
        _jwtSettings = jwtOptions.Value;
        _rsa = rsaKey;
    }

    // This method builds and signs a JWT token with the RSA private key.
    // All configured audiences are embedded in the token's aud claim as an array.
    // Each downstream service validates against its own specific audience name.
    // 'purpose' is an optional custom claim used to scope the token (e.g. "password-reset").
    // 'expiryMinutesOverride' allows issuing short-lived tokens (e.g. 15 min for password reset).
    public string GenerateToken(Guid userId, string email, IList<string> roles, string? purpose = null, int? expiryMinutesOverride = null)
    {
        // Pack user identity and roles into claims — these are readable by any service that validates the token
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),  // subject — who this token belongs to
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // unique token ID — used for blacklisting
        };

        // Each role is added as a separate claim — ASP.NET reads these for [Authorize(Roles = "Admin")]
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        // Optional purpose claim — scopes the token to a specific action like password reset
        if (!string.IsNullOrEmpty(purpose))
            claims.Add(new Claim("purpose", purpose));

        // Embed all audiences as individual aud claims — JWT spec supports aud as an array
        // e.g. aud: ["IdentityAudience", "PolicyAudience", "ClaimsAudience", "AdminAudience"]
        // Each service then checks if its own audience name exists in this array
        foreach (var audience in _jwtSettings.Audiences)
            claims.Add(new Claim(JwtRegisteredClaimNames.Aud, audience));

        // Sign the token with the RSA private key using RS256 algorithm
        // Only the Identity service can sign — other services can only verify using the public key
        var rsaKey = new RsaSecurityKey(_rsa);
        var credentials = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256);

        int expiry = expiryMinutesOverride ?? _jwtSettings.ExpiryMinutes;

        // Audience is set via claims above — do not set it in the descriptor to avoid duplication
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expiry),
            Issuer = _jwtSettings.Issuer,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        // Returns the final compact JWT string: base64(header).base64(payload).base64(signature)
        return tokenHandler.WriteToken(token);
    }

    // Validates the token signature and expiry using the same RSA key.
    // Returns the ClaimsPrincipal (with all claims) if valid, null if invalid or expired.
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var rsaKey = new RsaSecurityKey(_rsa);

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = false, // refresh token validation doesn't check audience
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = rsaKey,
                ClockSkew = TimeSpan.Zero // no tolerance on expiry
            };

            return tokenHandler.ValidateToken(token, validationParams, out _);
        }
        catch
        {
            return null; // invalid signature, expired, or malformed
        }
    }
}
