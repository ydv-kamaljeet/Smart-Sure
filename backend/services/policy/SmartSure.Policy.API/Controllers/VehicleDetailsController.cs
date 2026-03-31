using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSure.Policy.Application.DTOs;
using SmartSure.Policy.Application.Interfaces;

namespace SmartSure.Policy.API.Controllers;

[ApiController]
[Route("api/policy/vehicle-details")]
[Authorize]
public class VehicleDetailsController : ControllerBase
{
    private readonly IVehicleDetailsService _service;

    public VehicleDetailsController(IVehicleDetailsService service)
    {
        _service = service;
    }

    [HttpGet("{policyId}")]
    public async Task<IActionResult> GetVehicleDetails(Guid policyId)
    {
        var result = await _service.GetDetailsAsync(policyId);
        if (!result.IsSuccess) return NotFound(new { result.ErrorMessage });
        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> CreateVehicleDetails([FromQuery] Guid policyId, [FromBody] CreateVehicleDetailsDto dto)
    {
        var result = await _service.CreateDetailsAsync(policyId, dto);
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return Ok(new { Id = result.Data, Message = "Vehicle details created." });
    }

    [HttpPut("{policyId}")]
    public async Task<IActionResult> UpdateVehicleDetails(Guid policyId, [FromBody] UpdateVehicleDetailsDto dto)
    {
        var result = await _service.UpdateDetailsAsync(policyId, dto);
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return Ok(new { Message = "Vehicle details updated." });
    }
}
