using SmartSure.Shared.Common.Models;

namespace SmartSure.Admin.Domain.Entities;

public class AdminPolicy : BaseEntity
{
    public Guid PolicyId { get; set; } // Map to Policy Service ID
    public string PolicyNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string InsuranceType { get; set; } = string.Empty; // Health, Vehicle, Home
    public decimal PremiumAmount { get; set; }
    public string Status { get; set; } = "Active"; // Active, Cancelled, Expired
}
