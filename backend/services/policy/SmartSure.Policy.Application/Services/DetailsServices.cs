using SmartSure.Policy.Application.DTOs;
using SmartSure.Policy.Application.Interfaces;
using SmartSure.Shared.Common.Models;

namespace SmartSure.Policy.Application.Services;

public class HomeDetailsService : IHomeDetailsService
{
    private readonly IPolicyRepository _policyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public HomeDetailsService(IPolicyRepository policyRepository, IUnitOfWork unitOfWork)
    {
        _policyRepository = policyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<HomeDetailsDto>> GetDetailsAsync(Guid policyId)
    {
        var details = await _policyRepository.GetHomeDetailsAsync(policyId);
        if (details == null) return Result<HomeDetailsDto>.Failure("Details not found.");

        return Result<HomeDetailsDto>.Success(new HomeDetailsDto(
            details.Id, details.PropertyAddress, details.PropertyValue, details.YearBuilt,
            details.ConstructionType, details.HasSecuritySystem, details.HasFireAlarm));
    }

    public async Task<Result<Guid>> CreateDetailsAsync(Guid policyId, CreateHomeDetailsDto dto)
    {
        var exists = await _policyRepository.GetHomeDetailsAsync(policyId);
        if (exists != null) return Result<Guid>.Failure("Details already exist for this policy.");

        var details = new Domain.Entities.HomeDetails
        {
            Id = Guid.NewGuid(),
            PolicyId = policyId,
            PropertyAddress = dto.PropertyAddress,
            PropertyValue = dto.PropertyValue,
            YearBuilt = dto.YearBuilt,
            ConstructionType = dto.ConstructionType,
            HasSecuritySystem = dto.HasSecuritySystem,
            HasFireAlarm = dto.HasFireAlarm
        };

        await _policyRepository.AddHomeDetailsAsync(details);
        await _unitOfWork.SaveChangesAsync();

        return Result<Guid>.Success(details.Id);
    }

    public async Task<Result> UpdateDetailsAsync(Guid policyId, UpdateHomeDetailsDto dto)
    {
        var details = await _policyRepository.GetHomeDetailsAsync(policyId);
        if (details == null) return Result.Failure("Details not found.");

        details.PropertyAddress = dto.PropertyAddress;
        details.PropertyValue = dto.PropertyValue;
        details.YearBuilt = dto.YearBuilt;
        details.ConstructionType = dto.ConstructionType;
        details.HasSecuritySystem = dto.HasSecuritySystem;
        details.HasFireAlarm = dto.HasFireAlarm;
        details.UpdatedAt = DateTime.UtcNow;

        await _policyRepository.UpdateHomeDetailsAsync(details);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }
}

public class VehicleDetailsService : IVehicleDetailsService
{
    private readonly IPolicyRepository _policyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public VehicleDetailsService(IPolicyRepository policyRepository, IUnitOfWork unitOfWork)
    {
        _policyRepository = policyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<VehicleDetailsDto>> GetDetailsAsync(Guid policyId)
    {
        var details = await _policyRepository.GetVehicleDetailsAsync(policyId);
        if (details == null) return Result<VehicleDetailsDto>.Failure("Details not found.");

        return Result<VehicleDetailsDto>.Success(new VehicleDetailsDto(
            details.Id, details.Make, details.Model, details.Year, details.ListedPrice, details.Vin, details.LicensePlate, details.AnnualMileage));
    }

    public async Task<Result<Guid>> CreateDetailsAsync(Guid policyId, CreateVehicleDetailsDto dto)
    {
        var exists = await _policyRepository.GetVehicleDetailsAsync(policyId);
        if (exists != null) return Result<Guid>.Failure("Details already exist for this policy.");

        var details = new Domain.Entities.VehicleDetails
        {
            Id = Guid.NewGuid(),
            PolicyId = policyId,
            Make = dto.Make,
            Model = dto.Model,
            Year = dto.Year,
            ListedPrice = dto.ListedPrice,
            Vin = dto.Vin,
            LicensePlate = dto.LicensePlate,
            AnnualMileage = dto.AnnualMileage
        };

        await _policyRepository.AddVehicleDetailsAsync(details);
        await _unitOfWork.SaveChangesAsync();

        return Result<Guid>.Success(details.Id);
    }

    public async Task<Result> UpdateDetailsAsync(Guid policyId, UpdateVehicleDetailsDto dto)
    {
        var details = await _policyRepository.GetVehicleDetailsAsync(policyId);
        if (details == null) return Result.Failure("Details not found.");

        details.Make = dto.Make;
        details.Model = dto.Model;
        details.Year = dto.Year;
        details.ListedPrice = dto.ListedPrice;
        details.Vin = dto.Vin;
        details.LicensePlate = dto.LicensePlate;
        details.AnnualMileage = dto.AnnualMileage;
        details.UpdatedAt = DateTime.UtcNow;

        await _policyRepository.UpdateVehicleDetailsAsync(details);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }
}
