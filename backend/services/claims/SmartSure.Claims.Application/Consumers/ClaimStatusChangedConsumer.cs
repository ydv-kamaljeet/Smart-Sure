using MassTransit;
using Microsoft.Extensions.Logging;
using SmartSure.Claims.Application.Interfaces;
using SmartSure.Claims.Domain.Entities;
using SmartSure.Shared.Contracts.Events;

namespace SmartSure.Claims.Application.Consumers;

public class ClaimStatusChangedConsumer : IConsumer<ClaimStatusChangedEvent>
{
    private readonly IClaimRepository _claimRepository;
    private readonly IClaimHistoryRepository _historyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClaimStatusChangedConsumer> _logger;

    public ClaimStatusChangedConsumer(
        IClaimRepository claimRepository,
        IClaimHistoryRepository historyRepository,
        IUnitOfWork unitOfWork,
        ILogger<ClaimStatusChangedConsumer> logger)
    {
        _claimRepository = claimRepository;
        _historyRepository = historyRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ClaimStatusChangedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Claims Service: Received ClaimStatusChangedEvent for ClaimId {ClaimId} → {NewStatus}", message.ClaimId, message.NewStatus);

        var claim = await _claimRepository.GetClaimByIdAsync(message.ClaimId);
        if (claim == null)
        {
            _logger.LogWarning("Claims Service: ClaimId {ClaimId} not found for status update.", message.ClaimId);
            return;
        }

        if (claim.Status == message.NewStatus)
        {
            _logger.LogInformation("Claims Service: ClaimId {ClaimId} already in status {Status}. Skipping.", message.ClaimId, message.NewStatus);
            return;
        }

        var oldStatus = claim.Status;
        claim.Status = message.NewStatus;
        claim.UpdatedAt = DateTime.UtcNow;

        await _claimRepository.UpdateClaimAsync(claim);
        await _historyRepository.AddHistoryTokenAsync(new ClaimHistory
        {
            ClaimId = claim.Id,
            Claim = claim,
            PreviousStatus = oldStatus,
            NewStatus = message.NewStatus,
            ChangedByUserId = Guid.Empty, // system/admin triggered
            Remarks = $"Status changed from {oldStatus} to {message.NewStatus}",
            ChangedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Claims Service: ClaimId {ClaimId} status updated to {NewStatus}.", message.ClaimId, message.NewStatus);
    }
}
