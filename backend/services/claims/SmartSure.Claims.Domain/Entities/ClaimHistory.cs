using System;
using SmartSure.Shared.Common.Models;

namespace SmartSure.Claims.Domain.Entities;

public class ClaimHistory : BaseEntity
{
    public int ClaimId { get; set; }
    public Claim? Claim { get; set; }

    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    
    public Guid ChangedByUserId { get; set; }
    public string Remarks { get; set; } = string.Empty; // Optional notes from admin or system
    
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
