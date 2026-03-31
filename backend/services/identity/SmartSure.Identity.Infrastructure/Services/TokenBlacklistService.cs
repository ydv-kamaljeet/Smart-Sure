using Microsoft.Extensions.Caching.Memory;
using SmartSure.Identity.Application.Interfaces;

namespace SmartSure.Identity.Infrastructure.Services;

// TokenBlacklistService invalidates JWT tokens after logout or after a password-reset token is used.
// Uses in-memory cache (IMemoryCache) — no DB table needed. Tokens auto-expire from cache matching their JWT lifetime.
// Registered as Singleton because IMemoryCache is a singleton and the blacklist must persist across requests.
// Limitation: in-memory only — if the server restarts or scales to multiple instances, the blacklist is lost.
// For production multi-instance deployments, replace IMemoryCache with a distributed cache like Redis.
public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly IMemoryCache _cache;

    public TokenBlacklistService(IMemoryCache cache)
    {
        _cache = cache;
    }

    // Adds the token to the blacklist cache with a TTL matching the token's remaining lifetime.
    // After the TTL expires, the cache entry is automatically removed (no cleanup needed).
    public Task BlacklistTokenAsync(string token, TimeSpan expiry)
    {
        // Key is prefixed with "blacklist:" to avoid collisions with other cache entries
        _cache.Set($"blacklist:{token}", true, expiry);
        return Task.CompletedTask;
    }

    // Checks if a token is in the blacklist. Called by the JWT middleware on every authenticated request.
    public Task<bool> IsBlacklistedAsync(string token)
    {
        return Task.FromResult(_cache.TryGetValue($"blacklist:{token}", out _));
    }
    
}
