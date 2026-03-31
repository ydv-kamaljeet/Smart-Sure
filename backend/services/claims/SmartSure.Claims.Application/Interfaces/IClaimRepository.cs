using System.Collections.Generic;
using System.Threading.Tasks;
using SmartSure.Claims.Domain.Entities;

namespace SmartSure.Claims.Application.Interfaces;

public interface IClaimRepository
{
    Task<IEnumerable<Claim>> GetClaimsByUserIdAsync(Guid userId, int page, int pageSize, string? status);
    Task<int> GetClaimsCountAsync(Guid userId, string? status);
    Task<Claim?> GetClaimByIdAsync(int id);
    Task<IEnumerable<Claim>> GetClaimsByPolicyIdAsync(Guid policyId);
    Task AddClaimAsync(Claim claim);
    Task UpdateClaimAsync(Claim claim);
    Task<Dictionary<string, int>> GetClaimSummaryAsync(Guid userId);
    
    Task<ValidPolicy?> GetValidPolicyAsync(Guid policyId);
    Task AddValidPolicyAsync(ValidPolicy policy);
    Task UpdateValidPolicyAsync(ValidPolicy policy);
}
