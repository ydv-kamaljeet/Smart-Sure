using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SmartSure.Claims.Application.DTOs;
using SmartSure.Claims.Application.Interfaces;
using SmartSure.Claims.Domain.Entities;
using SmartSure.Shared.Common.Models;
using SmartSure.Shared.Contracts.Events;
using MassTransit;

namespace SmartSure.Claims.Application.Services;

public class ClaimManagementService : IClaimManagementService
{
    private readonly IClaimRepository _claimRepository;
    private readonly IClaimHistoryRepository _historyRepository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IUnitOfWork _unitOfWork;

    public ClaimManagementService(
        IClaimRepository claimRepository, 
        IClaimHistoryRepository historyRepository, 
        IPublishEndpoint publishEndpoint, 
        IUnitOfWork unitOfWork)
    {
        _claimRepository = claimRepository;
        _historyRepository = historyRepository;
        _publishEndpoint = publishEndpoint;
        _unitOfWork = unitOfWork;
    }

    public async Task<ClaimDto?> GetClaimByIdAsync(int claimId)
    {
        var claim = await _claimRepository.GetClaimByIdAsync(claimId);
        if (claim == null) return null;

        return MapToDto(claim);
    }

    public async Task<IEnumerable<ClaimHistoryDto>> GetClaimHistoryAsync(int claimId)
    {
        var history = await _historyRepository.GetHistoryByClaimIdAsync(claimId);
        return history.Select(h => new ClaimHistoryDto(h.Id, h.ClaimId, h.PreviousStatus, h.NewStatus, h.ChangedByUserId, h.Remarks, h.ChangedAt));
    }

    public async Task<PagedResult<ClaimDto>> GetClaimsAsync(Guid userId, int page, int pageSize, string? status)
    {
        var claims = await _claimRepository.GetClaimsByUserIdAsync(userId, page, pageSize, status);
        var totalCount = await _claimRepository.GetClaimsCountAsync(userId, status);
        var dtos = claims.Select(MapToDto).ToList();
        return new PagedResult<ClaimDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<ClaimDto>> GetClaimsByPolicyIdAsync(Guid policyId)
    {
        var claims = await _claimRepository.GetClaimsByPolicyIdAsync(policyId);
        return claims.Select(MapToDto);
    }

    public async Task<Dictionary<string, int>> GetClaimSummaryAsync(Guid userId)
    {
        return await _claimRepository.GetClaimSummaryAsync(userId);
    }

    public async Task<Result<ClaimDto>> InitiateClaimAsync(Guid userId, CreateClaimDto dto)
    {
        // Policy validation
        var validPolicy = await _claimRepository.GetValidPolicyAsync(dto.PolicyId);
        
        if (validPolicy == null)
            return Result<ClaimDto>.Failure("Invalid Policy ID. The policy does not exist.");
        
        if (validPolicy.UserId != userId)
            return Result<ClaimDto>.Failure("Unauthorized. You do not own this policy.");

        if (validPolicy.Status == "Cancelled")
            return Result<ClaimDto>.Failure("Cannot file a claim against a cancelled policy.");

        // Amount validation
        if (dto.ClaimAmount <= 0)
            return Result<ClaimDto>.Failure("Claim amount must be greater than 0.");

        if (validPolicy.InsuredDeclaredValue > 0 && dto.ClaimAmount > validPolicy.InsuredDeclaredValue)
            return Result<ClaimDto>.Failure($"Claim amount cannot exceed the policy IDV of ₹{validPolicy.InsuredDeclaredValue:N0}.");

        // Incident date validation
        var today = DateTime.UtcNow.Date;
        var incidentDate = dto.IncidentDate.Date;

        if (incidentDate > today)
            return Result<ClaimDto>.Failure("Incident date cannot be in the future.");

        if (validPolicy.StartDate.HasValue && incidentDate < validPolicy.StartDate.Value.Date)
            return Result<ClaimDto>.Failure($"Incident date cannot be before the policy start date ({validPolicy.StartDate.Value:MMM d, yyyy}).");

        if (validPolicy.EndDate.HasValue && incidentDate > validPolicy.EndDate.Value.Date)
            return Result<ClaimDto>.Failure($"Incident date cannot be after the policy end date ({validPolicy.EndDate.Value:MMM d, yyyy}).");

        var claim = new Claim
        {
            UserId = userId,
            PolicyId = dto.PolicyId,
            IncidentDate = dto.IncidentDate,
            Description = dto.Description,
            ClaimAmount = dto.ClaimAmount,
            Status = "Submitted",
            ClaimNumber = "CLM-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper()
        };

        await _claimRepository.AddClaimAsync(claim);
        
        await RecordHistoryAsync(claim, "", "Submitted", userId, "Claim initiated and submitted.");
        
        await _unitOfWork.SaveChangesAsync();

        // Publish Event
        var claimSubmittedEvent = new ClaimSubmittedEvent(claim.Id, claim.PolicyId, claim.UserId, validPolicy.PolicyNumber, claim.ClaimNumber, claim.ClaimAmount, claim.IncidentDate, "", "Submitted", validPolicy.CustomerName);
        await _publishEndpoint.Publish(claimSubmittedEvent);

        return Result<ClaimDto>.Success(MapToDto(claim));
    }

    public async Task<Result> ProcessClaimAsync(int claimId, Guid adminId, bool approve, string remarks)
    {
        var claim = await _claimRepository.GetClaimByIdAsync(claimId);
        if (claim == null) return Result.Failure("Claim not found.");

        if (claim.Status != "Submitted" && claim.Status != "UnderReview")
        {
            return Result.Failure($"Cannot process claim in status: {claim.Status}");
        }

        var oldStatus = claim.Status;
        claim.Status = approve ? "Approved" : "Rejected";
        claim.UpdatedAt = DateTime.UtcNow;

        await _claimRepository.UpdateClaimAsync(claim);
        await RecordHistoryAsync(claim, oldStatus, claim.Status, adminId, remarks);

        // Publish Event depending on status
        if (approve)
        {
            await _publishEndpoint.Publish(new ClaimApprovedEvent(claim.Id, claim.PolicyId, claim.UserId, claim.ClaimAmount, remarks));
        }
        else
        {
            await _publishEndpoint.Publish(new ClaimRejectedEvent(claim.Id, claim.PolicyId, claim.UserId, remarks));
        }

        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> SubmitClaimAsync(int claimId, Guid userId)
    {
        var claim = await _claimRepository.GetClaimByIdAsync(claimId);
        if (claim == null) return Result.Failure("Claim not found.");

        if (claim.UserId != userId) return Result.Failure("Unauthorized.");
        
        if (claim.Status != "Draft") return Result.Failure("Only Draft claims can be submitted.");

        var oldStatus = claim.Status;
        claim.Status = "Submitted";
        claim.UpdatedAt = DateTime.UtcNow;

        await _claimRepository.UpdateClaimAsync(claim);
        await RecordHistoryAsync(claim, oldStatus, claim.Status, userId, "Claim submitted by user.");

        var policy = await _claimRepository.GetValidPolicyAsync(claim.PolicyId);
        var policyNumber = policy?.PolicyNumber ?? "Unknown";

        var claimSubmittedEvent = new ClaimSubmittedEvent(claim.Id, claim.PolicyId, claim.UserId, policyNumber, claim.ClaimNumber, claim.ClaimAmount, claim.IncidentDate, "Draft", "Submitted");
        await _publishEndpoint.Publish(claimSubmittedEvent);

        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> UpdateDraftClaimAsync(int claimId, Guid userId, UpdateClaimDto dto)
    {
        var claim = await _claimRepository.GetClaimByIdAsync(claimId);
        if (claim == null) return Result.Failure("Claim not found.");

        if (claim.UserId != userId) return Result.Failure("Unauthorized.");
        if (claim.Status != "Draft") return Result.Failure("Only Draft claims can be updated.");

        claim.IncidentDate = dto.IncidentDate;
        claim.Description = dto.Description;
        claim.ClaimAmount = dto.ClaimAmount;
        claim.UpdatedAt = DateTime.UtcNow;

        await _claimRepository.UpdateClaimAsync(claim);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> WithdrawClaimAsync(int claimId, Guid userId)
    {
        var claim = await _claimRepository.GetClaimByIdAsync(claimId);
        if (claim == null) return Result.Failure("Claim not found.");

        if (claim.UserId != userId) return Result.Failure("Unauthorized.");
        if (claim.Status != "Draft" && claim.Status != "Submitted") return Result.Failure("Only Draft or Submitted claims can be withdrawn.");

        var oldStatus = claim.Status;
        claim.Status = "Withdrawn";
        claim.UpdatedAt = DateTime.UtcNow;

        await _claimRepository.UpdateClaimAsync(claim);
        await RecordHistoryAsync(claim, oldStatus, claim.Status, userId, "Claim withdrawn by user.");
        
        var statusChangedEvent = new ClaimStatusChangedEvent(claim.Id, claim.PolicyId, oldStatus, claim.Status);
        await _publishEndpoint.Publish(statusChangedEvent);

        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    private async Task RecordHistoryAsync(Claim claim, string oldStatus, string newStatus, Guid changedBy, string remarks)
    {
        var history = new ClaimHistory
        {
            ClaimId = claim.Id,
            Claim = claim,
            PreviousStatus = oldStatus,
            NewStatus = newStatus,
            ChangedByUserId = changedBy,
            Remarks = remarks,
            ChangedAt = DateTime.UtcNow
        };
        await _historyRepository.AddHistoryTokenAsync(history);
    }

    private ClaimDto MapToDto(Claim c)
    {
        return new ClaimDto(
            c.Id,
            c.ClaimNumber,
            c.UserId,
            c.PolicyId,
            c.IncidentDate,
            c.Description,
            c.ClaimAmount,
            c.Status,
            c.CreatedAt,
            c.Documents?.Select(d => new ClaimDocumentDto(d.Id, d.ClaimId, d.DocumentType, d.FileName, d.FileUrl, d.UploadedAt)),
            c.History?.Select(h => new ClaimHistoryDto(h.Id, h.ClaimId, h.PreviousStatus, h.NewStatus, h.ChangedByUserId, h.Remarks, h.ChangedAt))
        );
    }
}
