using MassTransit;
using Microsoft.Extensions.Logging;
using SmartSure.Admin.Application.Interfaces;
using SmartSure.Admin.Domain.Entities;
using SmartSure.Shared.Contracts.Events;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSure.Admin.Application.Consumers;

public class UserLoggedInConsumer : IConsumer<UserLoggedInEvent>
{
    private readonly IAdminRepository<AdminUser> _userRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserLoggedInConsumer> _logger;

    public UserLoggedInConsumer(
        IAdminRepository<AdminUser> userRepo,
        IUnitOfWork unitOfWork,
        ILogger<UserLoggedInConsumer> logger)
    {
        _userRepo = userRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserLoggedInEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received UserLoggedInEvent for UserId {UserId}", message.UserId);

        var existingUsers = await _userRepo.GetAllAsync();
        var localUser = existingUsers.FirstOrDefault(u => u.UserId == message.UserId);

        if (localUser != null)
        {
            localUser.LastLogin = message.LoggedInAt;
            await _userRepo.UpdateAsync(localUser);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Updated LastLogin for UserId {UserId}.", message.UserId);
        }
    }
}
