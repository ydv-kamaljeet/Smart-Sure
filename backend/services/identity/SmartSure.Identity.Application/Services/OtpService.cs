using BCrypt.Net;
using SmartSure.Identity.Application.Interfaces;
using SmartSure.Identity.Domain.Entities;
using SmartSure.Shared.Common.Models;

namespace SmartSure.Identity.Application.Services;

// OtpService handles generation and validation of one-time passwords used in the forgot-password flow.
public class OtpService : IOtpService
{
    private readonly IOtpRepository _otpRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OtpService(IOtpRepository otpRepository, IUnitOfWork unitOfWork)
    {
        _otpRepository = otpRepository;
        _unitOfWork = unitOfWork;
    }

    // Generates a random 6-digit OTP, BCrypt-hashes it, and stores it in the DB with a 10-minute expiry.
    // If an OTP already exists for this email, it is deleted first — only one active OTP per email at a time.
    // Returns the plain OTP code so the caller (AuthService) can email it to the user.
    public async Task<Result<string>> GenerateOtpAsync(string email)
    {
        // Delete any existing OTP for this email before creating a new one
        var existing = await _otpRepository.GetByEmailAsync(email);
        if (existing != null)
        {
            await _otpRepository.DeleteAsync(existing);
        }

        // Generate a random 6-digit code (100000–999999)
        var random = new Random();
        var otpCode = random.Next(100000, 999999).ToString();

        // Hash the OTP before storing — same BCrypt approach as passwords, never store plain OTPs
        var hashed = BCrypt.Net.BCrypt.HashPassword(otpCode);

        var record = new OtpRecord
        {
            Email = email,
            HashedOtp = hashed,
            Expiry = DateTime.UtcNow.AddMinutes(10), // OTP is valid for 10 minutes only
            Attempts = 0
        };

        await _otpRepository.AddAsync(record);
        await _unitOfWork.SaveChangesAsync();

        // Return the plain OTP — only time it's ever in plain text, immediately emailed and discarded
        return Result<string>.Success(otpCode);
    }

    // Validates the submitted OTP against the stored BCrypt hash.
    // Enforces expiry check and a max of 3 failed attempts before invalidating the OTP entirely.
    // Deletes the OTP record after successful validation — one-time use only.
    public async Task<Result> ValidateOtpAsync(string email, string otpCode)
    {
        var record = await _otpRepository.GetByEmailAsync(email);
        if (record == null)
            return Result.Failure("OTP not found or expired.");

        // Check if OTP has passed its 10-minute expiry window
        if (record.Expiry < DateTime.UtcNow)
        {
            await _otpRepository.DeleteAsync(record);
            await _unitOfWork.SaveChangesAsync();
            return Result.Failure("OTP expired.");
        }

        // Lock out after 3 failed attempts to prevent brute-force guessing
        if (record.Attempts >= 3)
        {
            await _otpRepository.DeleteAsync(record);
            await _unitOfWork.SaveChangesAsync();
            return Result.Failure("Too many failed attempts. OTP invalidated.");
        }

        // Verify the submitted code against the stored BCrypt hash
        if (!BCrypt.Net.BCrypt.Verify(otpCode, record.HashedOtp))
        {
            record.Attempts++; // increment attempt counter on each wrong guess
            await _otpRepository.UpdateAsync(record);
            await _unitOfWork.SaveChangesAsync();
            return Result.Failure("Invalid OTP.");
        }

        // OTP is valid — delete it immediately so it cannot be reused
        await _otpRepository.DeleteAsync(record);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }
}
