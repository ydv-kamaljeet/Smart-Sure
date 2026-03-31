using SmartSure.Policy.Domain.Entities;

namespace SmartSure.Policy.Application.Interfaces;

public interface IInsuranceCatalogRepository
{
    // Types
    Task<IEnumerable<InsuranceType>> GetAllTypesAsync(bool activeOnly = true);
    Task<InsuranceType?> GetTypeByIdAsync(int id);
    Task<InsuranceType?> GetTypeByNameAsync(string name);
    Task AddTypeAsync(InsuranceType type);
    Task UpdateTypeAsync(InsuranceType type);
    
    // SubTypes
    Task<IEnumerable<InsuranceSubType>> GetSubTypesByTypeIdAsync(int typeId, bool activeOnly = true);
    Task<InsuranceSubType?> GetSubTypeByIdAsync(int id);
    Task AddSubTypeAsync(InsuranceSubType subType);
    Task UpdateSubTypeAsync(InsuranceSubType subType);
}
