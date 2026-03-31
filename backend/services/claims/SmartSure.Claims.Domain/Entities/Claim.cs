using System;
using System.Collections.Generic;
using SmartSure.Shared.Common.Models;

namespace SmartSure.Claims.Domain.Entities;

public class Claim : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid PolicyId { get; set; }
    
    // Core claim details
    public DateTime IncidentDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal ClaimAmount { get; set; }
    
    public string Status { get; set; } = "Draft"; // Draft, Submitted, UnderReview, Approved, Rejected, Withdrawn
    public string ClaimNumber { get; set; } = string.Empty;
    
    // Optional navigation properties for aggregate management in EF 
    // (If using a strict DDD approach, these might be loaded separately, but EF Core collection navigation is standard for rapid CRUD)
    public ICollection<ClaimDocument> Documents { get; set; } = new List<ClaimDocument>();
    public ICollection<ClaimHistory> History { get; set; } = new List<ClaimHistory>();
}
