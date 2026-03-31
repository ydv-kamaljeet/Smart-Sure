using MassTransit;
using Microsoft.Extensions.Logging;
using SmartSure.Admin.Application.Interfaces;
using SmartSure.Admin.Domain.Entities;
using SmartSure.Shared.Contracts.Events;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSure.Admin.Application.Consumers;

public class PolicyCreatedConsumer : IConsumer<PolicyCreatedEvent>
{
    private readonly IAdminRepository<AdminPolicy> _policyRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PolicyCreatedConsumer> _logger;

    public PolicyCreatedConsumer(
        IAdminRepository<AdminPolicy> policyRepo,
        IUnitOfWork unitOfWork,
        ILogger<PolicyCreatedConsumer> logger)
    {
        _policyRepo = policyRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PolicyCreatedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received PolicyCreatedEvent for PolicyId: {PolicyId}", message.PolicyId);

        var existingPolicies = await _policyRepo.GetAllAsync();
        var localPolicy = existingPolicies.FirstOrDefault(p => p.PolicyId == message.PolicyId);

        if (localPolicy == null)
        {
            var adminPolicy = new AdminPolicy
            {
                PolicyId = message.PolicyId,
                PolicyNumber = message.PolicyNumber,
                CustomerName = message.CustomerName,
                InsuranceType = message.InsuranceType,
                PremiumAmount = message.PremiumAmount,
                Status = message.Status
            };

            await _policyRepo.AddAsync(adminPolicy);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Successfully mirrored PolicyId {PolicyId} into Admin DB.", message.PolicyId);
        }
        else
        {
            _logger.LogWarning("PolicyId {PolicyId} already exists in Admin DB. Skipping creation.", message.PolicyId);
        }
    }
}
