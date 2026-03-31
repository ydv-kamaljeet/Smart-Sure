using SmartSure.Policy.Application.DTOs;
using SmartSure.Shared.Common.Models;

namespace SmartSure.Policy.Application.Interfaces;

public interface IPolicyManagementService
{
    Task<PagedResult<PolicyDto>> GetPoliciesByUserIdAsync(Guid userId, int page, int pageSize);
    Task<Result<PolicyDetailDto>> GetPolicyDetailAsync(Guid policyId, Guid userId);
    Task<Result<Guid>> BuyPolicyAsync(Guid userId, BuyPolicyDto dto);
    Task<Result> CancelPolicyAsync(Guid policyId); // Admin only
    
    // Premium Calculation
    Task<Result<decimal>> CalculatePremiumAsync(Guid policyId, Guid userId);

    // Document Storage (Mock)
    Task<Result<PolicyDocumentDto>> GetPolicyDetailsDocumentAsync(Guid policyId, Guid userId);
    Task<Result> SavePolicyDetailsDocumentAsync(Guid policyId, Guid userId, UploadDocumentDto dto);
    Task<Result> UpdatePolicyDetailsDocumentAsync(Guid policyId, Guid userId, UploadDocumentDto dto);
}
