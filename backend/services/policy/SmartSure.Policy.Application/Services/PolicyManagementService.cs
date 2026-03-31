using SmartSure.Policy.Application.DTOs;
using SmartSure.Policy.Application.Interfaces;
using SmartSure.Shared.Common.Models;
using MassTransit;
using System.Text.Json;
using SmartSure.Shared.Contracts.Events;

namespace SmartSure.Policy.Application.Services;

public class PolicyManagementService : IPolicyManagementService
{
    private readonly IPolicyRepository _policyRepository;
    private readonly IInsuranceCatalogRepository _catalogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public PolicyManagementService(
        IPolicyRepository policyRepository, 
        IInsuranceCatalogRepository catalogRepository,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint)
    {
        _policyRepository = policyRepository;
        _catalogRepository = catalogRepository;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<PagedResult<PolicyDto>> GetPoliciesByUserIdAsync(Guid userId, int page, int pageSize)
    {
        var result = await _policyRepository.GetPoliciesByUserIdAsync(userId, page, pageSize);
        
        var dtos = result.Items.Select(p => new PolicyDto(
            p.Id, 
            p.PolicyNumber,
            p.Status, 
            p.PremiumAmount,
            p.InsuredDeclaredValue,
            p.StartDate, 
            p.EndDate,
            p.InsuranceSubType != null ? new InsuranceSubTypeDto(
                p.InsuranceSubType.Id, p.InsuranceSubType.InsuranceTypeId, p.InsuranceSubType.Name, 
                p.InsuranceSubType.Description, p.InsuranceSubType.BasePremium, p.InsuranceSubType.IsActive) : null
        ));

        return new PagedResult<PolicyDto>
        {
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            Items = dtos
        };
    }

    public async Task<Result<PolicyDetailDto>> GetPolicyDetailAsync(Guid policyId, Guid userId)
    {
        var policy = await _policyRepository.GetPolicyByIdAndUserIdAsync(policyId, userId);
        if (policy == null) return Result<PolicyDetailDto>.Failure("Policy not found.");

        var dto = new PolicyDetailDto(
            policy.Id, policy.PolicyNumber, policy.Status, policy.PremiumAmount, policy.InsuredDeclaredValue,
            policy.StartDate, policy.EndDate,
            policy.InsuranceSubType != null ? new InsuranceSubTypeDto(
                policy.InsuranceSubType.Id, policy.InsuranceSubType.InsuranceTypeId, policy.InsuranceSubType.Name, 
                policy.InsuranceSubType.Description, policy.InsuranceSubType.BasePremium, policy.InsuranceSubType.IsActive) : null,
            policy.HomeDetails != null ? new HomeDetailsDto(
                policy.HomeDetails.Id, policy.HomeDetails.PropertyAddress, policy.HomeDetails.PropertyValue, 
                policy.HomeDetails.YearBuilt, policy.HomeDetails.ConstructionType, policy.HomeDetails.HasSecuritySystem, policy.HomeDetails.HasFireAlarm) : null,
            policy.VehicleDetails != null ? new VehicleDetailsDto(
                policy.VehicleDetails.Id, policy.VehicleDetails.Make, policy.VehicleDetails.Model, 
                policy.VehicleDetails.Year, policy.VehicleDetails.ListedPrice, policy.VehicleDetails.Vin, policy.VehicleDetails.LicensePlate, policy.VehicleDetails.AnnualMileage) : null
        );

        return Result<PolicyDetailDto>.Success(dto);
    }

    public async Task<Result<Guid>> BuyPolicyAsync(Guid userId, BuyPolicyDto dto)
    {
        var subType = await _catalogRepository.GetSubTypeByIdAsync(dto.SubTypeId);
        if (subType == null || !subType.IsActive) 
            return Result<Guid>.Failure("Invalid or inactive insurance product.");

        // Calculate IDV (matches frontend formula exactly)
        decimal idv = 0;
        decimal premium = 0;

        if (dto.VehicleDetails != null)
        {
            // Vehicle IDV: listed price minus depreciation by age (matches frontend formula)
            var vehicleAge = DateTime.UtcNow.Year - dto.VehicleDetails.Year;
            var depreciationRate = vehicleAge switch
            {
                < 1  => 0.05m,
                < 2  => 0.15m,
                < 3  => 0.20m,
                < 4  => 0.30m,
                < 5  => 0.40m,
                _    => 0.50m
            };
            idv = Math.Round(dto.VehicleDetails.ListedPrice * (1 - depreciationRate), 2);
            // Premium = 2% of IDV
            premium = Math.Round(idv * 0.02m, 2);
            if (dto.VehicleDetails.AnnualMileage > 15000) premium = Math.Round(premium * 1.2m, 2); // 20% surcharge
        }
        else if (dto.HomeDetails != null)
        {
            // Home IDV: reconstruction cost (80% of market value) minus age depreciation
            // Matches IRDAI practice — IDV never equals full market value
            var buildingAge = DateTime.UtcNow.Year - dto.HomeDetails.YearBuilt;
            var depreciationRate = buildingAge switch
            {
                < 5  => 0.10m,
                < 10 => 0.20m,
                < 20 => 0.30m,
                < 30 => 0.40m,
                _    => 0.50m
            };
            var reconstructionCost = dto.HomeDetails.PropertyValue * 0.80m;
            idv = Math.Round(reconstructionCost * (1 - depreciationRate), 2);
            // Premium = 0.1% of IDV
            premium = Math.Round(idv * 0.001m, 2);
            if (dto.HomeDetails.HasSecuritySystem) premium = Math.Round(premium * 0.9m, 2); // 10% discount
        }
        else
        {
            // Fallback for other types
            idv = subType.BasePremium * 50m;
            premium = subType.BasePremium;
        }

        var policy = new Domain.Entities.Policy
        {
            Id = Guid.NewGuid(),
            PolicyNumber = $"POL-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
            UserId = userId,
            InsuranceSubTypeId = subType.Id,
            Status = "Active",
            PremiumAmount = premium,
            InsuredDeclaredValue = idv,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddYears(1)
        };

        if (dto.HomeDetails != null)
        {
            policy.HomeDetails = new Domain.Entities.HomeDetails
            {
                Id = Guid.NewGuid(),
                PolicyId = policy.Id,
                PropertyAddress = dto.HomeDetails.PropertyAddress,
                PropertyValue = dto.HomeDetails.PropertyValue,
                YearBuilt = dto.HomeDetails.YearBuilt,
                ConstructionType = dto.HomeDetails.ConstructionType,
                HasSecuritySystem = dto.HomeDetails.HasSecuritySystem,
                HasFireAlarm = dto.HomeDetails.HasFireAlarm
            };
        }

        if (dto.VehicleDetails != null)
        {
            policy.VehicleDetails = new Domain.Entities.VehicleDetails
            {
                Id = Guid.NewGuid(),
                PolicyId = policy.Id,
                Make = dto.VehicleDetails.Make,
                Model = dto.VehicleDetails.Model,
                Year = dto.VehicleDetails.Year,
                ListedPrice = dto.VehicleDetails.ListedPrice,
                Vin = dto.VehicleDetails.Vin,
                LicensePlate = dto.VehicleDetails.LicensePlate,
                AnnualMileage = dto.VehicleDetails.AnnualMileage
            };
        }

        await _policyRepository.AddPolicyAsync(policy);

        var holder = await _policyRepository.GetPolicyHolderAsync(userId);
        var customerName = holder?.FullName ?? "Unknown";

        // Publish PolicyCreatedEvent
        await _publishEndpoint.Publish(new PolicyCreatedEvent(
            policy.Id,
            policy.PolicyNumber,
            policy.UserId,
            customerName,
            subType.Name,
            policy.PremiumAmount,
            policy.InsuredDeclaredValue,
            policy.Status,
            DateTime.UtcNow,
            policy.StartDate,
            policy.EndDate
        ));

        await _unitOfWork.SaveChangesAsync();

        return Result<Guid>.Success(policy.Id);
    }

    public async Task<Result> CancelPolicyAsync(Guid policyId)
    {
        var policy = await _policyRepository.GetPolicyByIdAsync(policyId);
        if (policy == null) return Result.Failure("Policy not found.");
        if (policy.Status == "Cancelled") return Result.Failure("Policy is already cancelled.");

        policy.Status = "Cancelled";
        policy.UpdatedAt = DateTime.UtcNow;

        await _policyRepository.UpdatePolicyAsync(policy);

        await _publishEndpoint.Publish(new PolicyCancelledEvent(
            policy.Id,
            policy.UserId,
            "Admin Action",
            DateTime.UtcNow
        ));

        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<decimal>> CalculatePremiumAsync(Guid policyId, Guid userId)
    {
        var policy = await _policyRepository.GetPolicyByIdAndUserIdAsync(policyId, userId);
        if (policy == null) return Result<decimal>.Failure("Policy not found.");
        
        // Exposing stored premium for now. Real world might do real-time recalcs.
        return Result<decimal>.Success(policy.PremiumAmount);
    }

    public async Task<Result<PolicyDocumentDto>> GetPolicyDetailsDocumentAsync(Guid policyId, Guid userId)
    {
        var policy = await _policyRepository.GetPolicyByIdAndUserIdAsync(policyId, userId);
        if (policy == null) return Result<PolicyDocumentDto>.Failure("Policy not found.");

        var doc = await _policyRepository.GetLatestPolicyDocumentAsync(policyId);
        if (doc == null) return Result<PolicyDocumentDto>.Failure("No document found for this policy.");

        return Result<PolicyDocumentDto>.Success(new PolicyDocumentDto(doc.Id, doc.DocumentUrl, doc.FileName, doc.ContentType, doc.FileSize, doc.UploadedAt));
    }

    public async Task<Result> SavePolicyDetailsDocumentAsync(Guid policyId, Guid userId, UploadDocumentDto dto)
    {
        var policy = await _policyRepository.GetPolicyByIdAndUserIdAsync(policyId, userId);
        if (policy == null) return Result.Failure("Policy not found.");

        // Mocking saving to blob storage
        var fakeUrl = $"https://smartsurestorage.blob.core.windows.net/policies/{policyId}/{dto.FileName}";

        var doc = new Domain.Entities.PolicyDocument
        {
            Id = Guid.NewGuid(),
            PolicyId = policyId,
            FileName = dto.FileName,
            ContentType = dto.ContentType,
            FileSize = dto.FileSize,
            DocumentUrl = fakeUrl
        };

        await _policyRepository.AddPolicyDocumentAsync(doc);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> UpdatePolicyDetailsDocumentAsync(Guid policyId, Guid userId, UploadDocumentDto dto)
    {
        // For simplicity, treating update as upload of a new version.
        return await SavePolicyDetailsDocumentAsync(policyId, userId, dto);
    }
}
