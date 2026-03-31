using MassTransit;
using Microsoft.Extensions.Logging;
using SmartSure.Admin.Application.Interfaces;
using SmartSure.Admin.Domain.Entities;
using SmartSure.Shared.Contracts.Events;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSure.Admin.Application.Consumers;

public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
{
    private readonly IAdminRepository<AdminUser> _userRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserRegisteredConsumer> _logger;

    public UserRegisteredConsumer(
        IAdminRepository<AdminUser> userRepo,
        IUnitOfWork unitOfWork,
        ILogger<UserRegisteredConsumer> logger)
    {
        _userRepo = userRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received UserRegisteredEvent for UserId {UserId} - Email: {Email}", message.UserId, message.Email);

        var existingUsers = await _userRepo.GetAllAsync();
        var localUser = existingUsers.FirstOrDefault(u => u.UserId == message.UserId);

        if (localUser == null)
        {
            var adminUser = new AdminUser
            {
                UserId = message.UserId,
                FullName = message.FullName,
                Email = message.Email,
                Role = message.Role,
                IsActive = true,
                CreatedAt = message.CreatedAt
            };

            await _userRepo.AddAsync(adminUser);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Successfully mirrored new user {Email} in Admin DB.", message.Email);
        }
        else
        {
            _logger.LogWarning("UserId {UserId} already exists in Admin DB! Ignoring.", message.UserId);
        }
    }
}
