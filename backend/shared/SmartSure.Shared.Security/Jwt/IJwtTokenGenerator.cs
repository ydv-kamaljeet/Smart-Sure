using System.Security.Claims;

namespace SmartSure.Shared.Security.Jwt;

public interface IJwtTokenGenerator
{
    string GenerateToken(Guid userId, string email, IList<string> roles, string? purpose = null, int? expiryMinutesOverride = null);

    // Validates a JWT token signature and expiry — returns the ClaimsPrincipal if valid, null if invalid
    ClaimsPrincipal? ValidateToken(string token);
}
