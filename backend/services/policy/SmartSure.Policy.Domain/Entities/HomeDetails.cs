namespace SmartSure.Policy.Domain.Entities;

// Note: Naming explicitly to avoid conflict with standard `SmartSure.Policy.Domain.Entities.Policy` in future
public class HomeDetails
{
    public Guid Id { get; set; }
    public Guid PolicyId { get; set; }
    
    public string PropertyAddress { get; set; } = string.Empty;
    public decimal PropertyValue { get; set; }
    public int YearBuilt { get; set; }
    public string? ConstructionType { get; set; }
    public bool HasSecuritySystem { get; set; }
    public bool HasFireAlarm { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Policy? Policy { get; set; }
}
