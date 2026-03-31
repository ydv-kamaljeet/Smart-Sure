using SmartSure.Shared.Common.Models;

namespace SmartSure.Identity.Application.Interfaces;

public interface ITokenBlacklistService
{
    Task BlacklistTokenAsync(string token, TimeSpan expiry);
    Task<bool> IsBlacklistedAsync(string token);
}
