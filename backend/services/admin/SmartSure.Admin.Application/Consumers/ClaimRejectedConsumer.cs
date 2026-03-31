using MassTransit;
using Microsoft.Extensions.Logging;
using SmartSure.Admin.Application.Interfaces;
using SmartSure.Admin.Domain.Entities;
using SmartSure.Shared.Contracts.Events;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSure.Admin.Application.Consumers;

public class ClaimRejectedConsumer : IConsumer<ClaimRejectedEvent>
{
    private readonly IAdminRepository<AdminClaim> _claimRepo;
    private readonly IAdminRepository<AdminUser> _userRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<ClaimRejectedConsumer> _logger;

    public ClaimRejectedConsumer(
        IAdminRepository<AdminClaim> claimRepo,
        IAdminRepository<AdminUser> userRepo,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ILogger<ClaimRejectedConsumer> logger)
    {
        _claimRepo = claimRepo;
        _userRepo = userRepo;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ClaimRejectedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received ClaimRejectedEvent for ClaimId {ClaimId}", message.ClaimId);

        var existingClaims = await _claimRepo.GetAllAsync();
        var localClaim = existingClaims.FirstOrDefault(c => c.ClaimId == message.ClaimId);

        if (localClaim != null)
        {
            localClaim.Status = "Rejected";
            localClaim.UpdatedAt = DateTime.UtcNow;
            await _claimRepo.UpdateAsync(localClaim);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Updated ClaimId {ClaimId} to Rejected.", message.ClaimId);
        }

        // Send email notification
        var users = await _userRepo.GetAllAsync();
        var user = users.FirstOrDefault(u => u.UserId == message.UserId);
        if (user != null)
        {
            var claimNumber = localClaim?.ClaimNumber ?? $"CLM-{message.ClaimId}";
            var body = $@"
                <div style='font-family:sans-serif;max-width:600px;margin:auto;'>
                  <h2 style='color:#dc3545;'>Claim Rejected</h2>
                  <p>Dear <strong>{user.FullName}</strong>,</p>
                  <p>We regret to inform you that your claim <strong>{claimNumber}</strong> has been <strong>rejected</strong>.</p>
                  <p><strong>Reason:</strong> {message.Remarks}</p>
                  <p>If you have any questions, please contact our support team.</p>
                  <br/>
                  <p style='color:#6c757d;font-size:0.85rem;'>SmartSure Insurance</p>
                </div>";

            await _emailService.SendEmailAsync(user.Email, $"Claim {claimNumber} Rejected", body);
        }
    }
}
