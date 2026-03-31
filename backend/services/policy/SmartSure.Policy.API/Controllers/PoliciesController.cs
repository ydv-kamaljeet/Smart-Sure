using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SmartSure.Policy.Application.DTOs;
using SmartSure.Policy.Application.Interfaces;
using SmartSure.Shared.Common.Constants;

namespace SmartSure.Policy.API.Controllers;

[ApiController]
[Route("api/policy/policies")]
[Authorize]
public class PoliciesController : ControllerBase
{
    private readonly IPolicyManagementService _policyService;
    private readonly IPaymentService _paymentService;

    public PoliciesController(IPolicyManagementService policyService, IPaymentService paymentService)
    {
        _policyService = policyService;
        _paymentService = paymentService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // 5.2 Policies (7 Endpoints)
    
    [HttpGet]
    public async Task<IActionResult> GetMyPolicies([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var policies = await _policyService.GetPoliciesByUserIdAsync(GetUserId(), page, pageSize);
        return Ok(policies);
    }

    [HttpGet("{policyId}")]
    [ResponseCache(Duration = 60)]
    public async Task<IActionResult> GetPolicyDetail(Guid policyId)
    {
        var result = await _policyService.GetPolicyDetailAsync(policyId, GetUserId());
        if (!result.IsSuccess) return NotFound(new { result.ErrorMessage });
        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> BuyPolicy([FromBody] BuyPolicyDto dto)
    {
        var result = await _policyService.BuyPolicyAsync(GetUserId(), dto);
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return Ok(new { PolicyId = result.Data, Message = "Policy successfully purchased and activated." });
    }

    [HttpPut("{policyId}/cancel")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CancelPolicy(Guid policyId)
    {
        var result = await _policyService.CancelPolicyAsync(policyId);
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return Ok(new { Message = "Policy cancelled successfully." });
    }

    [HttpGet("{policyId}/details")]
    public async Task<IActionResult> GetPolicyDocument(Guid policyId)
    {
        var result = await _policyService.GetPolicyDetailsDocumentAsync(policyId, GetUserId());
        if (!result.IsSuccess) return NotFound(new { result.ErrorMessage });
        return Ok(result.Data); // In reality, this might return a FileResult downloading the blob
    }

    [HttpPost("{policyId}/details")]
    public async Task<IActionResult> UploadPolicyDocument(Guid policyId, [FromBody] UploadDocumentDto dto)
    {
        var result = await _policyService.SavePolicyDetailsDocumentAsync(policyId, GetUserId(), dto);
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return Ok(new { Message = "Document uploaded to storage." });
    }

    [HttpPut("{policyId}/details")]
    public async Task<IActionResult> UpdatePolicyDocument(Guid policyId, [FromBody] UploadDocumentDto dto)
    {
        var result = await _policyService.UpdatePolicyDetailsDocumentAsync(policyId, GetUserId(), dto);
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return Ok(new { Message = "Document updated in storage." });
    }

    // 5.3 Premiums & Payments (3 Endpoints)

    [HttpGet("{policyId}/premium")]
    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> GetPremium(Guid policyId)
    {
        var result = await _policyService.CalculatePremiumAsync(policyId, GetUserId());
        if (!result.IsSuccess) return NotFound(new { result.ErrorMessage });
        return Ok(new { PremiumAmount = result.Data });
    }

    [HttpPost("{policyId}/payments")]
    public async Task<IActionResult> RecordPayment(Guid policyId, [FromBody] CreatePaymentDto dto)
    {
        var result = await _paymentService.RecordPaymentAsync(policyId, GetUserId(), dto);
        if (!result.IsSuccess) return BadRequest(new { result.ErrorMessage });
        return Ok(result.Data);
    }

    [HttpGet("{policyId}/payments")]
    public async Task<IActionResult> GetPayments(Guid policyId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var payments = await _paymentService.GetPaymentsAsync(policyId, GetUserId(), page, pageSize);
        return Ok(payments);
    }
}
