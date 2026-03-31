using SmartSure.Policy.Domain.Entities;
using SmartSure.Shared.Common.Models;

namespace SmartSure.Policy.Application.Interfaces;

public interface IPolicyRepository
{
    Task<PagedResult<Domain.Entities.Policy>> GetPoliciesByUserIdAsync(Guid userId, int page, int pageSize);
    Task<Domain.Entities.Policy?> GetPolicyByIdAsync(Guid policyId);
    Task<Domain.Entities.Policy?> GetPolicyByIdAndUserIdAsync(Guid policyId, Guid userId);
    Task AddPolicyAsync(Domain.Entities.Policy policy);
    Task UpdatePolicyAsync(Domain.Entities.Policy policy);

    // Associated Details
    Task<HomeDetails?> GetHomeDetailsAsync(Guid policyId);
    Task AddHomeDetailsAsync(HomeDetails details);
    Task UpdateHomeDetailsAsync(HomeDetails details);

    Task<VehicleDetails?> GetVehicleDetailsAsync(Guid policyId);
    Task AddVehicleDetailsAsync(VehicleDetails details);
    Task UpdateVehicleDetailsAsync(VehicleDetails details);

    // Documents
    Task<PolicyDocument?> GetLatestPolicyDocumentAsync(Guid policyId);
    Task AddPolicyDocumentAsync(PolicyDocument document);
    Task UpdatePolicyDocumentAsync(PolicyDocument document);

    // Policy Holders
    Task<PolicyHolder?> GetPolicyHolderAsync(Guid userId);
    Task AddPolicyHolderAsync(PolicyHolder holder);
}
