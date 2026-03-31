using Microsoft.EntityFrameworkCore;
using SmartSure.Policy.Application.Interfaces;
using SmartSure.Policy.Domain.Entities;
using SmartSure.Policy.Infrastructure.Data;
using SmartSure.Shared.Common.Models;

namespace SmartSure.Policy.Infrastructure.Repositories;

public class PolicyRepository : IPolicyRepository
{
    private readonly PolicyDbContext _context;

    public PolicyRepository(PolicyDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<Domain.Entities.Policy>> GetPoliciesByUserIdAsync(Guid userId, int page, int pageSize)
    {
        var query = _context.Policies
                            .Include(p => p.InsuranceSubType)
                                .ThenInclude(st => st!.InsuranceType)
                            .Where(p => p.UserId == userId);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(p => p.CreatedAt)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();

        return new PagedResult<Domain.Entities.Policy>
        {
            TotalCount = total,
            Items = items,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Domain.Entities.Policy?> GetPolicyByIdAsync(Guid policyId)
    {
        return await _context.Policies
                             .Include(p => p.InsuranceSubType)
                                .ThenInclude(st => st!.InsuranceType)
                             .Include(p => p.HomeDetails)
                             .Include(p => p.VehicleDetails)
                             .FirstOrDefaultAsync(p => p.Id == policyId);
    }

    public async Task<Domain.Entities.Policy?> GetPolicyByIdAndUserIdAsync(Guid policyId, Guid userId)
    {
         return await _context.Policies
                             .Include(p => p.InsuranceSubType)
                                .ThenInclude(st => st!.InsuranceType)
                             .Include(p => p.HomeDetails)
                             .Include(p => p.VehicleDetails)
                             .FirstOrDefaultAsync(p => p.Id == policyId && p.UserId == userId);
    }

    public async Task AddPolicyAsync(Domain.Entities.Policy policy)
    {
        await _context.Policies.AddAsync(policy);
    }

    public Task UpdatePolicyAsync(Domain.Entities.Policy policy)
    {
        _context.Policies.Update(policy);
        return Task.CompletedTask;
    }

    public async Task<HomeDetails?> GetHomeDetailsAsync(Guid policyId)
    {
        return await _context.HomeDetails.FirstOrDefaultAsync(h => h.PolicyId == policyId);
    }

    public async Task AddHomeDetailsAsync(HomeDetails details)
    {
        await _context.HomeDetails.AddAsync(details);
    }

    public Task UpdateHomeDetailsAsync(HomeDetails details)
    {
        _context.HomeDetails.Update(details);
        return Task.CompletedTask;
    }

    public async Task<VehicleDetails?> GetVehicleDetailsAsync(Guid policyId)
    {
        return await _context.VehicleDetails.FirstOrDefaultAsync(v => v.PolicyId == policyId);
    }

    public async Task AddVehicleDetailsAsync(VehicleDetails details)
    {
        await _context.VehicleDetails.AddAsync(details);
    }

    public Task UpdateVehicleDetailsAsync(VehicleDetails details)
    {
        _context.VehicleDetails.Update(details);
        return Task.CompletedTask;
    }

    public async Task<PolicyDocument?> GetLatestPolicyDocumentAsync(Guid policyId)
    {
        return await _context.PolicyDocuments
                             .Where(d => d.PolicyId == policyId)
                             .OrderByDescending(d => d.UploadedAt)
                             .FirstOrDefaultAsync();
    }

    public async Task AddPolicyDocumentAsync(PolicyDocument document)
    {
        await _context.PolicyDocuments.AddAsync(document);
    }

    public Task UpdatePolicyDocumentAsync(PolicyDocument document)
    {
        _context.PolicyDocuments.Update(document);
        return Task.CompletedTask;
    }

    public async Task<PolicyHolder?> GetPolicyHolderAsync(Guid userId)
    {
        return await _context.PolicyHolders.FirstOrDefaultAsync(h => h.UserId == userId);
    }

    public async Task AddPolicyHolderAsync(PolicyHolder holder)
    {
        await _context.PolicyHolders.AddAsync(holder);
    }
}
