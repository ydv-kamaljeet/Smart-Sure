namespace SmartSure.Policy.Domain.Entities;

public class VehicleDetails
{
    public Guid Id { get; set; }
    public Guid PolicyId { get; set; }

    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal ListedPrice { get; set; }
    public string Vin { get; set; } = string.Empty; // Vehicle Identification Number
    public string LicensePlate { get; set; } = string.Empty;
    public int AnnualMileage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Policy? Policy { get; set; }
}
