using MassTransit;
using Microsoft.Extensions.Logging;
using SmartSure.Policy.Domain.Entities;
using SmartSure.Shared.Contracts.Events;
using SmartSure.Policy.Application.Interfaces;
using System.Threading.Tasks;

namespace SmartSure.Policy.Application.Consumers;

public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
{
    private readonly IPolicyRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserRegisteredConsumer> _logger;

    public UserRegisteredConsumer(
        IPolicyRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<UserRegisteredConsumer> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Policy Service: Received UserRegisteredEvent for {UserId}", message.UserId);

        var existing = await _repository.GetPolicyHolderAsync(message.UserId);
        if (existing == null)
        {
            var holder = new PolicyHolder
            {
                UserId = message.UserId,
                FullName = message.FullName,
                Email = message.Email
            };
            await _repository.AddPolicyHolderAsync(holder);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Policy Service: Created new PolicyHolder for {UserId}", message.UserId);
        }
        else
        {
            _logger.LogInformation("Policy Service: PolicyHolder {UserId} already exists.", message.UserId);
        }
    }
}
