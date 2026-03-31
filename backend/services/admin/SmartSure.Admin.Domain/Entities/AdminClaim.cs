using SmartSure.Shared.Common.Models;

namespace SmartSure.Admin.Domain.Entities;

public class AdminClaim : BaseEntity
{
    public int ClaimId { get; set; } // Map to Claims Service ID
    public Guid UserId { get; set; }
    public Guid PolicyId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public string ClaimNumber { get; set; } = string.Empty;
    public decimal ClaimAmount { get; set; }
    public string Status { get; set; } = string.Empty; // Initiated, Submitted, Under Review, Approved, Rejected, Cancelled
    public DateTime IncidentDate { get; set; }
}
