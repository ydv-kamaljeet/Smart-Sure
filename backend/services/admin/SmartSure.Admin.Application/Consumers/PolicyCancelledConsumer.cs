using MassTransit;
using Microsoft.Extensions.Logging;
using SmartSure.Admin.Application.Interfaces;
using SmartSure.Admin.Domain.Entities;
using SmartSure.Shared.Contracts.Events;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSure.Admin.Application.Consumers;

public class PolicyCancelledConsumer : IConsumer<PolicyCancelledEvent>
{
    private readonly IAdminRepository<AdminPolicy> _policyRepo;
    private readonly IAdminRepository<AdminUser> _userRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<PolicyCancelledConsumer> _logger;

    public PolicyCancelledConsumer(
        IAdminRepository<AdminPolicy> policyRepo,
        IAdminRepository<AdminUser> userRepo,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ILogger<PolicyCancelledConsumer> logger)
    {
        _policyRepo = policyRepo;
        _userRepo = userRepo;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PolicyCancelledEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Admin Service: Received PolicyCancelledEvent for PolicyId: {PolicyId}", message.PolicyId);

        var allPolicies = await _policyRepo.GetAllAsync();
        var adminPolicy = allPolicies.FirstOrDefault(p => p.PolicyId == message.PolicyId);

        if (adminPolicy != null)
        {
            adminPolicy.Status = "Cancelled";
            await _policyRepo.UpdateAsync(adminPolicy);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Admin Service: Marked AdminPolicy {PolicyId} as Cancelled.", message.PolicyId);
        }

        // Send email notification
        var users = await _userRepo.GetAllAsync();
        var user = users.FirstOrDefault(u => u.UserId == message.UserId);
        if (user != null)
        {
            var policyNumber = adminPolicy?.PolicyNumber ?? message.PolicyId.ToString();
            var body = $@"
                <div style='font-family:sans-serif;max-width:600px;margin:auto;'>
                  <h2 style='color:#dc3545;'>Policy Cancelled</h2>
                  <p>Dear <strong>{user.FullName}</strong>,</p>
                  <p>Your policy <strong>{policyNumber}</strong> has been <strong>cancelled</strong>.</p>
                  <p><strong>Reason:</strong> {message.Reason}</p>
                  <p>If you believe this is an error or have questions, please contact our support team.</p>
                  <br/>
                  <p style='color:#6c757d;font-size:0.85rem;'>SmartSure Insurance</p>
                </div>";

            await _emailService.SendEmailAsync(user.Email, $"Policy {policyNumber} Cancelled", body);
        }
    }
}
