using SmartSure.Policy.Application.DTOs;
using SmartSure.Shared.Common.Models;

namespace SmartSure.Policy.Application.Interfaces;

public interface IHomeDetailsService
{
    Task<Result<HomeDetailsDto>> GetDetailsAsync(Guid policyId);
    Task<Result<Guid>> CreateDetailsAsync(Guid policyId, CreateHomeDetailsDto dto);
    Task<Result> UpdateDetailsAsync(Guid policyId, UpdateHomeDetailsDto dto);
}

public interface IVehicleDetailsService
{
    Task<Result<VehicleDetailsDto>> GetDetailsAsync(Guid policyId);
    Task<Result<Guid>> CreateDetailsAsync(Guid policyId, CreateVehicleDetailsDto dto);
    Task<Result> UpdateDetailsAsync(Guid policyId, UpdateVehicleDetailsDto dto);
}
