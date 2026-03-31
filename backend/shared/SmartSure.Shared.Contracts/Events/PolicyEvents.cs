using System;

namespace SmartSure.Shared.Contracts.Events;

public record PolicyCreatedEvent(
    Guid PolicyId,
    string PolicyNumber,
    Guid UserId,
    string CustomerName,
    string InsuranceType,
    decimal PremiumAmount,
    decimal InsuredDeclaredValue,
    string Status,
    DateTime CreatedAt,
    DateTime? StartDate = null,
    DateTime? EndDate = null
);

public record PolicyCancelledEvent(
    Guid PolicyId,
    Guid UserId,
    string Reason,
    DateTime CancelledAt
);

public record PremiumPaidEvent(
    Guid PolicyId,
    decimal Amount,
    DateTime PaidAt
);
