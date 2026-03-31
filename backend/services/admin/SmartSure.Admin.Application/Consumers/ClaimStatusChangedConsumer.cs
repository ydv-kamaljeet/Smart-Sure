using MassTransit;
using Microsoft.Extensions.Logging;
using SmartSure.Admin.Application.Interfaces;
using SmartSure.Admin.Domain.Entities;
using SmartSure.Shared.Contracts.Events;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSure.Admin.Application.Consumers;

public class ClaimStatusChangedConsumer : IConsumer<ClaimStatusChangedEvent>
{
    private readonly IAdminRepository<AdminClaim> _claimRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClaimStatusChangedConsumer> _logger;

    public ClaimStatusChangedConsumer(
        IAdminRepository<AdminClaim> claimRepo,
        IUnitOfWork unitOfWork,
        ILogger<ClaimStatusChangedConsumer> logger)
    {
        _claimRepo = claimRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ClaimStatusChangedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received ClaimStatusChangedEvent: ClaimId {ClaimId} status updated to {NewStatus}", message.ClaimId, message.NewStatus);

        var existingClaims = await _claimRepo.GetAllAsync();
        var localClaim = existingClaims.FirstOrDefault(c => c.ClaimId == message.ClaimId);

        if (localClaim != null)
        {
            localClaim.Status = message.NewStatus;
            localClaim.UpdatedAt = DateTime.UtcNow;

            await _claimRepo.UpdateAsync(localClaim);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Updated mirrored ClaimId {ClaimId} status to {NewStatus}.", message.ClaimId, message.NewStatus);
        }
        else
        {
            _logger.LogWarning("Received status update for ClaimId {ClaimId} but it was not found in Admin database out-of-order delivery?", message.ClaimId);
        }
    }
}
