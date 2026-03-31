using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSure.Policy.Application.DTOs;
using SmartSure.Policy.Application.Interfaces;
using SmartSure.Shared.Common.Constants;

namespace SmartSure.Policy.API.Controllers;

[ApiController]
[Route("api/policy")]
public class InsuranceCatalogController : ControllerBase
{
    private readonly IInsuranceCatalogService _catalogService;

    public InsuranceCatalogController(IInsuranceCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet("insurance-types")]
    [Authorize]
    public async Task<IActionResult> GetInsuranceTypes()
    {
        var types = await _catalogService.GetAllTypesAsync();
        return Ok(types);
    }

    [HttpGet("insurance-types/admin")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetInsuranceTypesAdmin()
    {
        var types = await _catalogService.GetAllTypesAdminAsync();
        return Ok(types);
    }

    [HttpGet("insurance-types/{typeId}")]
    [Authorize]
    public async Task<IActionResult> GetInsuranceType(int typeId)
    {
        var type = await _catalogService.GetTypeByIdAsync(typeId);
        if (type == null) return NotFound();
        return Ok(type);
    }

    [HttpPost("insurance-types")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CreateInsuranceType([FromBody] CreateInsuranceTypeDto dto)
    {
        var result = await _catalogService.CreateTypeAsync(dto);
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return CreatedAtAction(nameof(GetInsuranceType), new { typeId = result.Data!.Id }, result.Data);
    }

    [HttpPut("insurance-types/{typeId}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> UpdateInsuranceType(int typeId, [FromBody] UpdateInsuranceTypeDto dto)
    {
        var result = await _catalogService.UpdateTypeAsync(typeId, dto);
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return NoContent();
    }

    [HttpDelete("insurance-types/{typeId}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> SoftDeleteInsuranceType(int typeId)
    {
        var result = await _catalogService.SoftDeleteTypeAsync(typeId);
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return NoContent();
    }

    [HttpGet("insurance-types/{typeId}/subtypes")]
    [Authorize]
    public async Task<IActionResult> GetSubTypes(int typeId)
    {
        var subTypes = await _catalogService.GetSubTypesByTypeIdAsync(typeId);
        return Ok(subTypes);
    }

    [HttpGet("insurance-types/{typeId}/subtypes/admin")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetSubTypesAdmin(int typeId)
    {
        var subTypes = await _catalogService.GetSubTypesByTypeIdAdminAsync(typeId);
        return Ok(subTypes);
    }

    [HttpPost("insurance-subtypes")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CreateSubType([FromBody] CreateInsuranceSubTypeDto dto)
    {
        var result = await _catalogService.CreateSubTypeAsync(dto);
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return Ok(result.Data);
    }

    [HttpPut("insurance-subtypes/{subTypeId}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> UpdateSubType(int subTypeId, [FromBody] UpdateInsuranceSubTypeDto dto)
    {
        var result = await _catalogService.UpdateSubTypeAsync(subTypeId, dto);
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return NoContent();
    }

    [HttpDelete("insurance-subtypes/{subTypeId}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> SoftDeleteSubType(int subTypeId)
    {
        var result = await _catalogService.SoftDeleteSubTypeAsync(subTypeId);
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return NoContent();
    }
}
