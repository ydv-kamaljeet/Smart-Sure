namespace SmartSure.Shared.Security.Jwt;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";
    public string Issuer { get; init; } = null!;

    // Identity service sets all audiences — token will carry all of them in the aud claim array
    // Each downstream service sets only its own audience name for validation
    public string[] Audiences { get; init; } = [];

    // RS256 requires paths to keys or raw key string
    public string PrivateKeyContent { get; init; } = null!;
    public string PublicKeyContent { get; init; } = null!;
    public int ExpiryMinutes { get; init; } = 60;
}
