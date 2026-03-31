namespace SmartSure.Policy.Application.DTOs;

public record PolicyDto(
    Guid Id, 
    string PolicyNumber,
    string Status, 
    decimal PremiumAmount,
    decimal InsuredDeclaredValue,
    DateTime? StartDate, 
    DateTime? EndDate,
    InsuranceSubTypeDto? SubType);

public record PolicyDetailDto(
    Guid Id,
    string PolicyNumber,
    string Status,
    decimal PremiumAmount,
    decimal InsuredDeclaredValue,
    DateTime? StartDate,
    DateTime? EndDate,
    InsuranceSubTypeDto? SubType,
    HomeDetailsDto? HomeDetails,
    VehicleDetailsDto? VehicleDetails);

public record BuyPolicyDto(
    int SubTypeId,
    CreateHomeDetailsDto? HomeDetails,
    CreateVehicleDetailsDto? VehicleDetails); // Wizard submit
