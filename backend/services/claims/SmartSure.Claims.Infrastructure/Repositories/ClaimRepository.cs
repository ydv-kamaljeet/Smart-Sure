using Microsoft.EntityFrameworkCore;
using SmartSure.Claims.Application.Interfaces;
using SmartSure.Claims.Domain.Entities;
using SmartSure.Claims.Infrastructure.Data;

namespace SmartSure.Claims.Infrastructure.Repositories;

public class ClaimRepository : IClaimRepository
{
    private readonly ClaimsDbContext _context;

    public ClaimRepository(ClaimsDbContext context)
    {
        _context = context;
    }

    public async Task AddClaimAsync(Claim claim)
    {
        await _context.Claims.AddAsync(claim);
    }

    public async Task<Claim?> GetClaimByIdAsync(int id)
    {
        return await _context.Claims
                             .Include(c => c.Documents)
                             .Include(c => c.History)
                             .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Claim>> GetClaimsByPolicyIdAsync(Guid policyId)
    {
        return await _context.Claims
                             .Where(c => c.PolicyId == policyId)
                             .OrderByDescending(c => c.CreatedAt)
                             .ToListAsync();
    }

    public async Task<int> GetClaimsCountAsync(Guid userId, string? status)
    {
        var query = _context.Claims.AsQueryable();
        if (userId != Guid.Empty) query = query.Where(c => c.UserId == userId);
        if (!string.IsNullOrEmpty(status)) query = query.Where(c => c.Status == status);
        return await query.CountAsync();
    }

    public async Task<IEnumerable<Claim>> GetClaimsByUserIdAsync(Guid userId, int page, int pageSize, string? status)
    {
        var query = _context.Claims.AsQueryable();
        
        // Admin or Service viewing everything? If userId != Guid.Empty, filter. 
        // For standard "my claims" endpoint, userId is provided.
        if (userId != Guid.Empty)
        {
            query = query.Where(c => c.UserId == userId);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(c => c.Status == status);
        }

        return await query.OrderByDescending(c => c.CreatedAt)
                          .Skip((page - 1) * pageSize)
                          .Take(pageSize)
                          .ToListAsync();
    }

    public async Task<Dictionary<string, int>> GetClaimSummaryAsync(Guid userId)
    {
        var query = _context.Claims.AsQueryable();
        
        if (userId != Guid.Empty)
        {
            query = query.Where(c => c.UserId == userId);
        }

        var summary = await query.GroupBy(c => c.Status)
                                 .Select(g => new { Status = g.Key, Count = g.Count() })
                                 .ToDictionaryAsync(x => x.Status, x => x.Count);
                                 
        return summary;
    }

    public Task UpdateClaimAsync(Claim claim)
    {
        _context.Claims.Update(claim);
        return Task.CompletedTask;
    }

    public async Task<ValidPolicy?> GetValidPolicyAsync(Guid policyId)
    {
        return await _context.ValidPolicies.FirstOrDefaultAsync(p => p.PolicyId == policyId);
    }

    public async Task AddValidPolicyAsync(ValidPolicy policy)
    {
        await _context.ValidPolicies.AddAsync(policy);
    }

    public Task UpdateValidPolicyAsync(ValidPolicy policy)
    {
        _context.ValidPolicies.Update(policy);
        return Task.CompletedTask;
    }
}
