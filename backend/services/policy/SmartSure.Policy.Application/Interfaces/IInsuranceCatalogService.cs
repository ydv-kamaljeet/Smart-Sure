using SmartSure.Policy.Application.DTOs;
using SmartSure.Shared.Common.Models;

namespace SmartSure.Policy.Application.Interfaces;

public interface IInsuranceCatalogService
{
    // Types
    Task<IEnumerable<InsuranceTypeDto>> GetAllTypesAsync();
    Task<IEnumerable<InsuranceTypeDto>> GetAllTypesAdminAsync();
    Task<InsuranceTypeDto?> GetTypeByIdAsync(int id);
    Task<Result<InsuranceTypeDto>> CreateTypeAsync(CreateInsuranceTypeDto dto);
    Task<Result> UpdateTypeAsync(int id, UpdateInsuranceTypeDto dto);
    Task<Result> SoftDeleteTypeAsync(int id);

    // SubTypes
    Task<IEnumerable<InsuranceSubTypeDto>> GetSubTypesByTypeIdAsync(int typeId);
    Task<IEnumerable<InsuranceSubTypeDto>> GetSubTypesByTypeIdAdminAsync(int typeId);
    Task<Result<InsuranceSubTypeDto>> CreateSubTypeAsync(CreateInsuranceSubTypeDto dto);
    Task<Result> UpdateSubTypeAsync(int id, UpdateInsuranceSubTypeDto dto);
    Task<Result> SoftDeleteSubTypeAsync(int id);
}
