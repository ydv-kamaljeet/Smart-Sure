using MassTransit;
using Microsoft.Extensions.Logging;
using SmartSure.Admin.Application.Interfaces;
using SmartSure.Admin.Domain.Entities;
using SmartSure.Shared.Contracts.Events;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSure.Admin.Application.Consumers;

public class ClaimSubmittedConsumer : IConsumer<ClaimSubmittedEvent>
{
    private readonly IAdminRepository<AdminClaim> _claimRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClaimSubmittedConsumer> _logger;

    public ClaimSubmittedConsumer(
        IAdminRepository<AdminClaim> claimRepo,
        IUnitOfWork unitOfWork,
        ILogger<ClaimSubmittedConsumer> logger)
    {
        _claimRepo = claimRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ClaimSubmittedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received ClaimSubmittedEvent for ClaimId: {ClaimId}", message.ClaimId);

        var existingClaims = await _claimRepo.GetAllAsync();
        var localClaim = existingClaims.FirstOrDefault(c => c.ClaimId == message.ClaimId);

        if (localClaim == null)
        {
            var adminClaim = new AdminClaim
            {
                ClaimId = message.ClaimId,
                UserId = message.UserId,
                PolicyId = message.PolicyId,
                CustomerName = !string.IsNullOrEmpty(message.CustomerName) ? message.CustomerName : "Unknown",
                PolicyNumber = message.PolicyNumber,
                ClaimNumber = message.ClaimNumber,
                ClaimAmount = message.ClaimAmount,
                Status = message.NewStatus,
                IncidentDate = message.IncidentDate,
                CreatedAt = DateTime.UtcNow
            };

            await _claimRepo.AddAsync(adminClaim);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Successfully mirrored ClaimId {ClaimId} into Admin DB.", message.ClaimId);
        }
        else
        {
            _logger.LogWarning("ClaimId {ClaimId} already exists in Admin DB. Skipping creation.", message.ClaimId);
        }
    }
}
