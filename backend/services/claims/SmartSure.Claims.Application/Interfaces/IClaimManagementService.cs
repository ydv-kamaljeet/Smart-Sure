using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartSure.Claims.Application.DTOs;
using SmartSure.Shared.Common.Models;

namespace SmartSure.Claims.Application.Interfaces;

public interface IClaimManagementService
{
    // Queries
    Task<PagedResult<ClaimDto>> GetClaimsAsync(Guid userId, int page, int pageSize, string? status);
    Task<ClaimDto?> GetClaimByIdAsync(int claimId);
    Task<IEnumerable<ClaimDto>> GetClaimsByPolicyIdAsync(Guid policyId);
    Task<IEnumerable<ClaimHistoryDto>> GetClaimHistoryAsync(int claimId);
    Task<Dictionary<string, int>> GetClaimSummaryAsync(Guid userId);

    // Commands
    Task<Result<ClaimDto>> InitiateClaimAsync(Guid userId, CreateClaimDto dto);
    Task<Result> UpdateDraftClaimAsync(int claimId, Guid userId, UpdateClaimDto dto);
    
    // Status Transitions
    Task<Result> SubmitClaimAsync(int claimId, Guid userId);
    Task<Result> WithdrawClaimAsync(int claimId, Guid userId);
    
    // Admin only
    Task<Result> ProcessClaimAsync(int claimId, Guid adminId, bool approve, string remarks);
}
