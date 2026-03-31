namespace SmartSure.Policy.Domain.Entities;

public class InsuranceSubType
{
    public int Id { get; set; }
    public int InsuranceTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePremium { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public InsuranceType? InsuranceType { get; set; }
}
