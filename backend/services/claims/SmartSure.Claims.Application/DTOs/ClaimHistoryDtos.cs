using System;

namespace SmartSure.Claims.Application.DTOs;

public record ClaimHistoryDto(
    int Id,
    int ClaimId,
    string PreviousStatus,
    string NewStatus,
    Guid ChangedByUserId,
    string Remarks,
    DateTime ChangedAt
);
