using MassTransit;
using Microsoft.Extensions.Logging;
using SmartSure.Claims.Application.Interfaces;
using SmartSure.Claims.Domain.Entities;
using SmartSure.Shared.Contracts.Events;

namespace SmartSure.Claims.Application.Consumers;

public class ClaimRejectedConsumer : IConsumer<ClaimRejectedEvent>
{
    private readonly IClaimRepository _claimRepository;
    private readonly IClaimHistoryRepository _historyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClaimRejectedConsumer> _logger;

    public ClaimRejectedConsumer(
        IClaimRepository claimRepository,
        IClaimHistoryRepository historyRepository,
        IUnitOfWork unitOfWork,
        ILogger<ClaimRejectedConsumer> logger)
    {
        _claimRepository = claimRepository;
        _historyRepository = historyRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ClaimRejectedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Claims Service: Received ClaimRejectedEvent for ClaimId {ClaimId}", message.ClaimId);

        var claim = await _claimRepository.GetClaimByIdAsync(message.ClaimId);
        if (claim == null)
        {
            _logger.LogWarning("Claims Service: ClaimId {ClaimId} not found for rejection update.", message.ClaimId);
            return;
        }

        if (claim.Status == "Rejected")
        {
            _logger.LogInformation("Claims Service: ClaimId {ClaimId} already Rejected. Skipping.", message.ClaimId);
            return;
        }

        var oldStatus = claim.Status;
        claim.Status = "Rejected";
        claim.UpdatedAt = DateTime.UtcNow;

        await _claimRepository.UpdateClaimAsync(claim);
        await _historyRepository.AddHistoryTokenAsync(new ClaimHistory
        {
            ClaimId = claim.Id,
            Claim = claim,
            PreviousStatus = oldStatus,
            NewStatus = "Rejected",
            ChangedByUserId = message.UserId,
            Remarks = message.Remarks,
            ChangedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Claims Service: ClaimId {ClaimId} status updated to Rejected.", message.ClaimId);
    }
}
