using System.Collections.Generic;
using System.Threading.Tasks;
using SmartSure.Claims.Domain.Entities;

namespace SmartSure.Claims.Application.Interfaces;

public interface IClaimHistoryRepository
{
    Task<IEnumerable<ClaimHistory>> GetHistoryByClaimIdAsync(int claimId);
    Task AddHistoryTokenAsync(ClaimHistory history);
}
