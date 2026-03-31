using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSure.Policy.Application.DTOs;
using SmartSure.Policy.Application.Interfaces;

namespace SmartSure.Policy.API.Controllers;

[ApiController]
[Route("api/policy/home-details")]
[Authorize]
public class HomeDetailsController : ControllerBase
{
    private readonly IHomeDetailsService _service;

    public HomeDetailsController(IHomeDetailsService service)
    {
        _service = service;
    }

    [HttpGet("{policyId}")]
    public async Task<IActionResult> GetHomeDetails(Guid policyId)
    {
        var result = await _service.GetDetailsAsync(policyId);
        if (!result.IsSuccess) return NotFound(new { result.ErrorMessage });
        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> CreateHomeDetails([FromQuery] Guid policyId, [FromBody] CreateHomeDetailsDto dto)
    {
        var result = await _service.CreateDetailsAsync(policyId, dto);
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return Ok(new { Id = result.Data, Message = "Home details created." });
    }

    [HttpPut("{policyId}")]
    public async Task<IActionResult> UpdateHomeDetails(Guid policyId, [FromBody] UpdateHomeDetailsDto dto)
    {
        var result = await _service.UpdateDetailsAsync(policyId, dto);
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return Ok(new { Message = "Home details updated." });
    }
}
