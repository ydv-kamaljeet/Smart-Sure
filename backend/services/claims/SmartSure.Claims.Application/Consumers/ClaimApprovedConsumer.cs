using MassTransit;
using Microsoft.Extensions.Logging;
using SmartSure.Claims.Application.Interfaces;
using SmartSure.Claims.Domain.Entities;
using SmartSure.Shared.Contracts.Events;

namespace SmartSure.Claims.Application.Consumers;

public class ClaimApprovedConsumer : IConsumer<ClaimApprovedEvent>
{
    private readonly IClaimRepository _claimRepository;
    private readonly IClaimHistoryRepository _historyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClaimApprovedConsumer> _logger;
    public string userName;

    public ClaimApprovedConsumer(
        IClaimRepository claimRepository,
        IClaimHistoryRepository historyRepository,
        IUnitOfWork unitOfWork,
        ILogger<ClaimApprovedConsumer> logger)
    {
        _claimRepository = claimRepository;
        _historyRepository = historyRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;

        
    }

    public async Task Consume(ConsumeContext<ClaimApprovedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Claims Service: Received ClaimApprovedEvent for ClaimId {ClaimId}", message.ClaimId);

        var claim = await _claimRepository.GetClaimByIdAsync(message.ClaimId);
        if (claim == null)
        {
            _logger.LogWarning("Claims Service: ClaimId {ClaimId} not found for approval update.", message.ClaimId);
            return;
        }

        if (claim.Status == "Approved")
        {
            _logger.LogInformation("Claims Service: ClaimId {ClaimId} already Approved. Skipping.", message.ClaimId);
            return;
        }

        var oldStatus = claim.Status;
        claim.Status = "Approved";
        claim.UpdatedAt = DateTime.UtcNow;
        //userName = claim.userName;




        await _claimRepository.UpdateClaimAsync(claim);
        await _historyRepository.AddHistoryTokenAsync(new ClaimHistory
        {
            ClaimId = claim.Id,
            Claim = claim,
            PreviousStatus = oldStatus,
            NewStatus = "Approved",
            ChangedByUserId = message.UserId,
            Remarks = message.Remarks,
            ChangedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Claims Service: ClaimId {ClaimId} status updated to Approved.", message.ClaimId);
    }
}
