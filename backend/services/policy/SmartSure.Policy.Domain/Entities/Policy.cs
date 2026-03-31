namespace SmartSure.Policy.Domain.Entities;

public class Policy
{
    public Guid Id { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; } // Links to Identity User
    public int InsuranceSubTypeId { get; set; }
    
    public string Status { get; set; } = "Pending"; // Active, Pending, Cancelled, Expired
    public decimal PremiumAmount { get; set; }
    public decimal InsuredDeclaredValue { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public InsuranceSubType? InsuranceSubType { get; set; }
    public HomeDetails? HomeDetails { get; set; }
    public VehicleDetails? VehicleDetails { get; set; }
    public ICollection<PaymentRecord> Payments { get; set; } = new List<PaymentRecord>();
}
