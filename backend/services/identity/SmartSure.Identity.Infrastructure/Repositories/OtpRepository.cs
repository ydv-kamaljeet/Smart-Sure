using Microsoft.EntityFrameworkCore;
using SmartSure.Identity.Application.Interfaces;
using SmartSure.Identity.Domain.Entities;
using SmartSure.Identity.Infrastructure.Data;

namespace SmartSure.Identity.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of IOtpRepository.
/// Handles persistence for OTP records used in the forgot-password flow.
/// Note: expiry filtering is done in the service layer (OtpService), not here —
/// the repository returns the record regardless of expiry status.
/// </summary>
public class OtpRepository : IOtpRepository
{
    private readonly IdentityDbContext _context;

    public OtpRepository(IdentityDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns the OTP record for the given email, or null if none exists.
    /// Only one record per email is allowed — old records are deleted before creating new ones.
    /// </summary>
    public async Task<OtpRecord?> GetByEmailAsync(string email)
    {
        return await _context.OtpRecords.FirstOrDefaultAsync(o => o.Email == email);
    }

    /// <summary>Stages a new OtpRecord for insertion (persisted on next SaveChangesAsync).</summary>
    public async Task AddAsync(OtpRecord record)
    {
        await _context.OtpRecords.AddAsync(record);
    }

    /// <summary>Marks an existing OtpRecord as modified — used to increment the Attempts counter.</summary>
    public Task UpdateAsync(OtpRecord record)
    {
        _context.OtpRecords.Update(record);
        return Task.CompletedTask;
    }

    /// <summary>Stages an OtpRecord for deletion — called after successful validation or expiry.</summary>
    public Task DeleteAsync(OtpRecord record)
    {
        _context.OtpRecords.Remove(record);
        return Task.CompletedTask;
    }
}
