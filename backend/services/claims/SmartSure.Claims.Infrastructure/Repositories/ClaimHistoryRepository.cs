using Microsoft.EntityFrameworkCore;
using SmartSure.Claims.Application.Interfaces;
using SmartSure.Claims.Domain.Entities;
using SmartSure.Claims.Infrastructure.Data;

namespace SmartSure.Claims.Infrastructure.Repositories;

public class ClaimHistoryRepository : IClaimHistoryRepository
{
    private readonly ClaimsDbContext _context;

    public ClaimHistoryRepository(ClaimsDbContext context)
    {
        _context = context;
    }

    public async Task AddHistoryTokenAsync(ClaimHistory history)
    {
        await _context.ClaimHistories.AddAsync(history);
    }

    public async Task<IEnumerable<ClaimHistory>> GetHistoryByClaimIdAsync(int claimId)
    {
        return await _context.ClaimHistories
                             .Where(h => h.ClaimId == claimId)
                             .OrderByDescending(h => h.ChangedAt)
                             .ToListAsync();
    }
}
