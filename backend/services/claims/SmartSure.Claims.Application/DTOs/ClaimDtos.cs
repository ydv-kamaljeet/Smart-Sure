using System;
using System.Collections.Generic;

namespace SmartSure.Claims.Application.DTOs;

public record ClaimDto(
    int Id,
    string ClaimNumber,
    Guid UserId,
    Guid PolicyId,
    DateTime IncidentDate,
    string Description,
    decimal ClaimAmount,
    string Status,
    DateTime CreatedAt,
    IEnumerable<ClaimDocumentDto>? Documents = null,
    IEnumerable<ClaimHistoryDto>? History = null
);

public record CreateClaimDto(
    Guid PolicyId,
    DateTime IncidentDate,
    string Description,
    decimal ClaimAmount
);

public record UpdateClaimDto(
    DateTime IncidentDate,
    string Description,
    decimal ClaimAmount
);

// Summary dictionary is usually just serialized directly, but we can make a DTO if we want complex aggregations
public record ClaimSummaryDto(
    string Status,
    int Count
);
