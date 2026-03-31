using SmartSure.Identity.Application.DTOs;
using SmartSure.Shared.Common.Models;

namespace SmartSure.Identity.Application.Interfaces;

public interface IGoogleAuthService
{
    string GetGoogleConsentUrl();
    Task<Result<LoginResponseDto>> HandleCallbackAsync(string authorizationCode);
}
