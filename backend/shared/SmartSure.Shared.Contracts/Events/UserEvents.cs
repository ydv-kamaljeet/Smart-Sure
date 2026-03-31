using System;

namespace SmartSure.Shared.Contracts.Events;

public record UserRegisteredEvent(
    Guid UserId,
    string FullName,
    string Email,
    string Role,
    DateTime CreatedAt
);

public record UserLoggedInEvent(
    Guid UserId,
    string Email,
    DateTime LoggedInAt
);

public record UserRoleChangedEvent(
    Guid UserId,
    string NewRole,
    DateTime ChangedAt
);
