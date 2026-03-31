namespace SmartSure.Policy.Application.DTOs;

public record HomeDetailsDto(Guid Id, string PropertyAddress, decimal PropertyValue, int YearBuilt, string? ConstructionType, bool HasSecuritySystem, bool HasFireAlarm);
public record CreateHomeDetailsDto(string PropertyAddress, decimal PropertyValue, int YearBuilt, string? ConstructionType, bool HasSecuritySystem, bool HasFireAlarm);
public record UpdateHomeDetailsDto(string PropertyAddress, decimal PropertyValue, int YearBuilt, string? ConstructionType, bool HasSecuritySystem, bool HasFireAlarm);

public record VehicleDetailsDto(Guid Id, string Make, string Model, int Year, decimal ListedPrice, string Vin, string LicensePlate, int AnnualMileage);
public record CreateVehicleDetailsDto(string Make, string Model, int Year, decimal ListedPrice, string Vin, string LicensePlate, int AnnualMileage);
public record UpdateVehicleDetailsDto(string Make, string Model, int Year, decimal ListedPrice, string Vin, string LicensePlate, int AnnualMileage);
