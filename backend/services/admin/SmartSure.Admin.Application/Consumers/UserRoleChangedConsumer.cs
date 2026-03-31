using MassTransit;
using Microsoft.Extensions.Logging;
using SmartSure.Admin.Application.Interfaces;
using SmartSure.Admin.Domain.Entities;
using SmartSure.Shared.Contracts.Events;

namespace SmartSure.Admin.Application.Consumers;

public class UserRoleChangedConsumer : IConsumer<UserRoleChangedEvent>
{
    private readonly IAdminRepository<AdminUser> _userRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAdminAuditLogService _auditLogService;
    private readonly ILogger<UserRoleChangedConsumer> _logger;

    public UserRoleChangedConsumer(IAdminRepository<AdminUser> userRepo, IUnitOfWork unitOfWork, IAdminAuditLogService auditLogService, ILogger<UserRoleChangedConsumer> logger)
    {
        _userRepo = userRepo;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserRoleChangedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Received UserRoleChangedEvent for UserId {UserId}, NewRole: {Role}", evt.UserId, evt.NewRole);

        var allUsers = await _userRepo.GetAllAsync();
        var user = allUsers.FirstOrDefault(u => u.UserId == evt.UserId);

        if (user == null)
        {
            _logger.LogWarning("AdminUser not found for UserId {UserId}", evt.UserId);
            return;
        }

        var oldRole = user.Role;
        user.Role = evt.NewRole;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        await _auditLogService.LogActionAsync(
            "Role Changed", "AdminUser", evt.UserId.ToString(),
            $"User {user.Email} role changed from '{oldRole}' to '{evt.NewRole}'.");

        _logger.LogInformation("Updated Role to {Role} for UserId {UserId}", evt.NewRole, evt.UserId);
    }
}
