using System;
using SmartSure.Shared.Common.Models;

namespace SmartSure.Claims.Domain.Entities;

public class ClaimDocument : BaseEntity
{
    public int ClaimId { get; set; }
    public Claim? Claim { get; set; } // Reference

    public string DocumentType { get; set; } = string.Empty; // e.g., "PoliceReport", "Photos", "Invoice"
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty; // URL to Blob Storage
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
