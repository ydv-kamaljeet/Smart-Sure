using MassTransit;
using SmartSure.Admin.Application.DTOs;
using SmartSure.Admin.Application.Interfaces;
using SmartSure.Admin.Domain.Entities;
using SmartSure.Shared.Common.Models;
using SmartSure.Shared.Contracts.Events;

namespace SmartSure.Admin.Application.Services;

public class AdminClaimsService : IAdminClaimsService
{
    private readonly IAdminRepository<AdminClaim> _claimRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAdminAuditLogService _auditLogService;
    private readonly IBus _bus;
    public AdminClaimsService(IAdminRepository<AdminClaim> claimRepo, IUnitOfWork unitOfWork, IAdminAuditLogService auditLogService, IBus bus)
    {
        _claimRepo = claimRepo;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _bus = bus;
    }

    public async Task<PagedResult<AdminClaimDto>> GetClaimsAsync(string? status, DateTime? fromDate, Guid? userId, int page, int pageSize)
    {
        var allClaims = await _claimRepo.GetAllAsync();
        var query = allClaims.AsQueryable();

        if (!string.IsNullOrEmpty(status)) query = query.Where(c => c.Status == status);
        if (fromDate.HasValue) query = query.Where(c => c.IncidentDate >= fromDate.Value);
        if (userId.HasValue) query = query.Where(c => c.UserId == userId.Value);

        var total = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize).Select(MapToDto).ToList();

        return new PagedResult<AdminClaimDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AdminClaimDto?> GetClaimByIdAsync(int claimId)
    {
        var claim = (await _claimRepo.GetAllAsync()).FirstOrDefault(c => c.ClaimId == claimId);
        return claim != null ? MapToDto(claim) : null;
    }

    public async Task<bool> MarkAsUnderReviewAsync(int claimId, string remarks)
    {
        var claim = (await _claimRepo.GetAllAsync()).FirstOrDefault(c => c.ClaimId == claimId);
        if (claim == null) return false;

        var oldStatus = claim.Status;
        claim.Status = "Under Review";
        claim.UpdatedAt = DateTime.UtcNow;
        await _claimRepo.UpdateAsync(claim);
        await _unitOfWork.SaveChangesAsync();
        
        await _auditLogService.LogActionAsync("Review Claim", "AdminClaim", claimId.ToString(), $"Claim {claim.PolicyNumber} marked as Under Review. Remarks: {remarks}");
        
        await _bus.Publish(new ClaimStatusChangedEvent(claim.ClaimId, claim.PolicyId, oldStatus, "Under Review"));
        return true;
    }

    public async Task<bool> ApproveClaimAsync(int claimId, string remarks)
    {
        var claim = (await _claimRepo.GetAllAsync()).FirstOrDefault(c => c.ClaimId == claimId);
        if (claim == null) return false;

        claim.Status = "Approved";
        claim.UpdatedAt = DateTime.UtcNow;
        await _claimRepo.UpdateAsync(claim);
        await _unitOfWork.SaveChangesAsync();
        
        await _auditLogService.LogActionAsync("Approve Claim", "AdminClaim", claimId.ToString(), $"Claim {claim.PolicyNumber} approved. Remarks: {remarks}");
        
        await _bus.Publish(new ClaimApprovedEvent(claim.ClaimId, claim.PolicyId, claim.UserId, claim.ClaimAmount, remarks));
        return true;
    }

    public async Task<bool> RejectClaimAsync(int claimId, string reason)
    {
        var claim = (await _claimRepo.GetAllAsync()).FirstOrDefault(c => c.ClaimId == claimId);
        if (claim == null) return false;

        claim.Status = "Rejected";
        claim.UpdatedAt = DateTime.UtcNow;
        await _claimRepo.UpdateAsync(claim);
        await _unitOfWork.SaveChangesAsync();
        
        await _auditLogService.LogActionAsync("Reject Claim", "AdminClaim", claimId.ToString(), $"Claim {claim.PolicyNumber} rejected. Reason: {reason}");
        
        await _bus.Publish(new ClaimRejectedEvent(claim.ClaimId, claim.PolicyId, claim.UserId, reason));
        return true;
    }

    private AdminClaimDto MapToDto(AdminClaim claim) => new AdminClaimDto
    {
        Id = claim.Id,
        ClaimId = claim.ClaimId,
        UserId = claim.UserId,
        CustomerName = claim.CustomerName,
        PolicyNumber = claim.PolicyNumber,
        ClaimNumber = claim.ClaimNumber,
        ClaimAmount = claim.ClaimAmount,
        Status = claim.Status,
        IncidentDate = claim.IncidentDate,
        CreatedAt = claim.CreatedAt
    };
}
