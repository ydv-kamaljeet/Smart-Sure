using System;

namespace SmartSure.Shared.Contracts.Events;

public record ClaimSubmittedEvent(int ClaimId, Guid PolicyId, Guid UserId, string PolicyNumber, string ClaimNumber, decimal ClaimAmount, DateTime IncidentDate, string OldStatus, string NewStatus, string CustomerName = "");

public record ClaimStatusChangedEvent(int ClaimId, Guid PolicyId, string OldStatus, string NewStatus);

public record ClaimApprovedEvent(int ClaimId, Guid PolicyId, Guid UserId, decimal ApprovedAmount, string Remarks);

public record ClaimRejectedEvent(int ClaimId, Guid PolicyId, Guid UserId, string Remarks);
