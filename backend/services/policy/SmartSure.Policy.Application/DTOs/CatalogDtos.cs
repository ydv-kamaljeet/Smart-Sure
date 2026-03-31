namespace SmartSure.Policy.Application.DTOs;

public record InsuranceTypeDto(int Id, string Name, string? Description, bool IsActive);
public record CreateInsuranceTypeDto(string Name, string? Description);
public record UpdateInsuranceTypeDto(string Name, string? Description, bool IsActive);

public record InsuranceSubTypeDto(int Id, int InsuranceTypeId, string Name, string? Description, decimal BasePremium, bool IsActive);
public record CreateInsuranceSubTypeDto(int InsuranceTypeId, string Name, string? Description, decimal BasePremium);
public record UpdateInsuranceSubTypeDto(string Name, string? Description, decimal BasePremium, bool IsActive);
