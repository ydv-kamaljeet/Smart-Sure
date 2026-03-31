namespace SmartSure.Policy.Application.DTOs;

public record PolicyDocumentDto(Guid Id, string DocumentUrl, string FileName, string ContentType, long FileSize, DateTime UploadedAt);

public record UploadDocumentDto(string FileName, string ContentType, long FileSize, string Base64Content); // Mock object representing a file upload
