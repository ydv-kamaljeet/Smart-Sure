using Microsoft.Extensions.Caching.Memory;
using SmartSure.Policy.Application.DTOs;
using SmartSure.Policy.Application.Interfaces;
using SmartSure.Policy.Domain.Entities;
using SmartSure.Shared.Common.Models;

namespace SmartSure.Policy.Application.Services;

public class InsuranceCatalogService : IInsuranceCatalogService
{
    private readonly IInsuranceCatalogRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;

    private const string AllTypesCacheKey = "insurance_types_all";
    private static string SubTypesCacheKey(int typeId) => $"insurance_subtypes_{typeId}";

    public InsuranceCatalogService(IInsuranceCatalogRepository repository, IUnitOfWork unitOfWork, IMemoryCache cache)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    // Types
    public async Task<IEnumerable<InsuranceTypeDto>> GetAllTypesAsync()
    {
        if (_cache.TryGetValue(AllTypesCacheKey, out IEnumerable<InsuranceTypeDto>? cached) && cached != null)
            return cached;

        var types = await _repository.GetAllTypesAsync(activeOnly: true);
        var result = types.Select(t => new InsuranceTypeDto(t.Id, t.Name, t.Description, t.IsActive));

        _cache.Set(AllTypesCacheKey, result, TimeSpan.FromMinutes(10));
        return result;
    }

    public async Task<IEnumerable<InsuranceTypeDto>> GetAllTypesAdminAsync()
    {
        // Admin view: return all types including inactive, no cache
        var types = await _repository.GetAllTypesAsync(activeOnly: false);
        return types.Select(t => new InsuranceTypeDto(t.Id, t.Name, t.Description, t.IsActive));
    }

    public async Task<InsuranceTypeDto?> GetTypeByIdAsync(int id)
    {
        var type = await _repository.GetTypeByIdAsync(id);
        if (type == null) return null;
        return new InsuranceTypeDto(type.Id, type.Name, type.Description, type.IsActive);
    }

    public async Task<Result<InsuranceTypeDto>> CreateTypeAsync(CreateInsuranceTypeDto dto)
    {
        var existing = await _repository.GetTypeByNameAsync(dto.Name);
        if (existing != null)
        {
            if (existing.IsActive)
                return Result<InsuranceTypeDto>.Failure("Insurance Type with this name already exists.");

            // Reactivate the inactive one instead of creating a duplicate
            existing.IsActive = true;
            existing.Description = dto.Description;
            existing.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateTypeAsync(existing);
            await _unitOfWork.SaveChangesAsync();
            _cache.Remove(AllTypesCacheKey);
            return Result<InsuranceTypeDto>.Success(new InsuranceTypeDto(existing.Id, existing.Name, existing.Description, existing.IsActive));
        }

        var newType = new InsuranceType
        {
            Name = dto.Name,
            Description = dto.Description,
            IsActive = true
        };

        await _repository.AddTypeAsync(newType);
        await _unitOfWork.SaveChangesAsync();
        _cache.Remove(AllTypesCacheKey);

        return Result<InsuranceTypeDto>.Success(new InsuranceTypeDto(newType.Id, newType.Name, newType.Description, newType.IsActive));
    }

    public async Task<Result> UpdateTypeAsync(int id, UpdateInsuranceTypeDto dto)
    {
        var type = await _repository.GetTypeByIdAsync(id);
        if (type == null) return Result.Failure("Insurance Type not found.");

        type.Name = dto.Name;
        type.Description = dto.Description;
        type.IsActive = dto.IsActive;
        type.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateTypeAsync(type);
        await _unitOfWork.SaveChangesAsync();
        _cache.Remove(AllTypesCacheKey);

        return Result.Success();
    }

    public async Task<Result> SoftDeleteTypeAsync(int id)
    {
        var type = await _repository.GetTypeByIdAsync(id);
        if (type == null) return Result.Failure("Insurance Type not found.");

        type.IsActive = false;
        type.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateTypeAsync(type);
        await _unitOfWork.SaveChangesAsync();
        _cache.Remove(AllTypesCacheKey);

        return Result.Success();
    }

    // SubTypes
    public async Task<IEnumerable<InsuranceSubTypeDto>> GetSubTypesByTypeIdAsync(int typeId)
    {
        var key = SubTypesCacheKey(typeId);
        if (_cache.TryGetValue(key, out IEnumerable<InsuranceSubTypeDto>? cached) && cached != null)
            return cached;

        var subTypes = await _repository.GetSubTypesByTypeIdAsync(typeId, activeOnly: true);
        var result = subTypes.Select(s => new InsuranceSubTypeDto(s.Id, s.InsuranceTypeId, s.Name, s.Description, s.BasePremium, s.IsActive));

        _cache.Set(key, result, TimeSpan.FromMinutes(10));
        return result;
    }

    public async Task<IEnumerable<InsuranceSubTypeDto>> GetSubTypesByTypeIdAdminAsync(int typeId)
    {
        var subTypes = await _repository.GetSubTypesByTypeIdAsync(typeId, activeOnly: false);
        return subTypes.Select(s => new InsuranceSubTypeDto(s.Id, s.InsuranceTypeId, s.Name, s.Description, s.BasePremium, s.IsActive));
    }

    public async Task<Result<InsuranceSubTypeDto>> CreateSubTypeAsync(CreateInsuranceSubTypeDto dto)
    {
        var type = await _repository.GetTypeByIdAsync(dto.InsuranceTypeId);
        if (type == null) return Result<InsuranceSubTypeDto>.Failure("Parent Insurance Type not found.");

        var newSubType = new InsuranceSubType
        {
            InsuranceTypeId = dto.InsuranceTypeId,
            Name = dto.Name,
            Description = dto.Description,
            BasePremium = dto.BasePremium,
            IsActive = true
        };

        await _repository.AddSubTypeAsync(newSubType);
        await _unitOfWork.SaveChangesAsync();
        _cache.Remove(SubTypesCacheKey(dto.InsuranceTypeId));

        return Result<InsuranceSubTypeDto>.Success(new InsuranceSubTypeDto(newSubType.Id, newSubType.InsuranceTypeId, newSubType.Name, newSubType.Description, newSubType.BasePremium, newSubType.IsActive));
    }

    public async Task<Result> UpdateSubTypeAsync(int id, UpdateInsuranceSubTypeDto dto)
    {
        var subType = await _repository.GetSubTypeByIdAsync(id);
        if (subType == null) return Result.Failure("Insurance SubType not found.");

        subType.Name = dto.Name;
        subType.Description = dto.Description;
        subType.BasePremium = dto.BasePremium;
        subType.IsActive = dto.IsActive;
        subType.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateSubTypeAsync(subType);
        await _unitOfWork.SaveChangesAsync();
        _cache.Remove(SubTypesCacheKey(subType.InsuranceTypeId));

        return Result.Success();
    }

    public async Task<Result> SoftDeleteSubTypeAsync(int id)
    {
        var subType = await _repository.GetSubTypeByIdAsync(id);
        if (subType == null) return Result.Failure("Insurance SubType not found.");

        subType.IsActive = false;
        subType.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateSubTypeAsync(subType);
        await _unitOfWork.SaveChangesAsync();
        _cache.Remove(SubTypesCacheKey(subType.InsuranceTypeId));

        return Result.Success();
    }
}
