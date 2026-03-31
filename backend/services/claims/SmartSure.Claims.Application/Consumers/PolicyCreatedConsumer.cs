using MassTransit;
using Microsoft.Extensions.Logging;
using SmartSure.Claims.Domain.Entities;
using SmartSure.Shared.Contracts.Events;
using SmartSure.Claims.Application.Interfaces;
using System.Threading.Tasks;

namespace SmartSure.Claims.Application.Consumers;

public class PolicyCreatedConsumer : IConsumer<PolicyCreatedEvent>
{
    private readonly IClaimRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PolicyCreatedConsumer> _logger;

    public PolicyCreatedConsumer(
        IClaimRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<PolicyCreatedConsumer> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PolicyCreatedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Claim Service: Received PolicyCreatedEvent for {PolicyId}", message.PolicyId);

        var existing = await _repository.GetValidPolicyAsync(message.PolicyId);
        if (existing == null)
        {
            var policy = new ValidPolicy
            {
                PolicyId = message.PolicyId,
                UserId = message.UserId,
                PolicyNumber = message.PolicyNumber,
                CustomerName = message.CustomerName,
                Status = message.Status,
                InsuredDeclaredValue = message.InsuredDeclaredValue,
                StartDate = message.StartDate,
                EndDate = message.EndDate
            };
            await _repository.AddValidPolicyAsync(policy);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Claim Service: Created ValidPolicy mirror for {PolicyId}", message.PolicyId);
        }
        else
        {
            _logger.LogInformation("Claim Service: ValidPolicy {PolicyId} already exists.", message.PolicyId);
        }
    }
}
