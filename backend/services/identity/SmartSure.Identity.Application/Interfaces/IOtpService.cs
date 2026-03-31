using SmartSure.Shared.Common.Models;

namespace SmartSure.Identity.Application.Interfaces;

public interface IOtpService
{
    Task<Result<string>> GenerateOtpAsync(string email);
    Task<Result> ValidateOtpAsync(string email, string otpCode);
}
