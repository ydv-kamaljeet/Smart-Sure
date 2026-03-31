using System;

namespace SmartSure.Claims.Application.DTOs;

public record ClaimDocumentDto(
    int Id,
    int ClaimId,
    string DocumentType,
    string FileName,
    string FileUrl,
    DateTime UploadedAt
);

public record UploadClaimDocumentDto(
    string DocumentType
);
