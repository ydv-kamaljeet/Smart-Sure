using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SmartSure.Policy.Application.Interfaces;
using System.Text.Json;
using System.Net.Http.Headers;

namespace SmartSure.Policy.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IPolicyManagementService _policyService;
    private readonly IHttpClientFactory _httpClientFactory;

    public DashboardController(IPolicyManagementService policyService, IHttpClientFactory httpClientFactory)
    {
        _policyService = policyService;
        _httpClientFactory = httpClientFactory;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetUserDashboard()
    {
        var userId = GetUserId();
        
        // 1. Get Policies
        var policiesResult = await _policyService.GetPoliciesByUserIdAsync(userId, 1, 100);
        var policies = policiesResult.Items ?? new List<Policy.Application.DTOs.PolicyDto>();

        // 2. Get Claims via HTTP Call to Claims API (or Gateway)
        // We pass the bearer token to the claims service
        var claims = new List<object>();
        try
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            
            var client = _httpClientFactory.CreateClient();
            if (!string.IsNullOrEmpty(authHeader))
            {
                client.DefaultRequestHeaders.Add("Authorization", authHeader);
            }
            
            // Assuming Claims Service runs on 5008 locally, or Gateway on 5083.
            // Best is to call the Claims service directly
            var response = await client.GetAsync("http://localhost:5008/api/claims");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // Use JsonDocument to parse dynamically since we don't share the DTO
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                if (root.TryGetProperty("items", out var itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in itemsElement.EnumerateArray())
                    {
                        var claimMap = new Dictionary<string, object>();
                        if (element.TryGetProperty("id", out var id)) claimMap["id"] = id.GetRawText();
                        if (element.TryGetProperty("policyNumber", out var polNum)) claimMap["policyId"] = polNum.GetString()!;
                        if (element.TryGetProperty("claimAmount", out var amt)) claimMap["amount"] = amt.GetDecimal();
                        if (element.TryGetProperty("status", out var status)) claimMap["status"] = status.GetString()!;
                        if (element.TryGetProperty("incidentDate", out var date)) claimMap["incidentDate"] = date.GetDateTime();
                        
                        claims.Add(claimMap);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log error, but don't fail the whole dashboard
            Console.WriteLine($"Error fetching claims for dashboard: {ex.Message}");
        }

        // 3. Assemble Dashboard Data
        var activePolicies = policies.Where(p => p.Status == "Active").ToList();
        var pendingClaims = claims.Where(c => 
        {
            if (c is Dictionary<string, object> dict && dict.ContainsKey("status"))
            {
                var status = dict["status"]?.ToString() ?? "";
                return status == "Pending" || status == "UnderReview" || status == "Initiated" || status == "Submitted";
            }
            return false;
        }).ToList();

        var result = new
        {
            Stats = new {
                ActivePolicies = activePolicies.Count,
                PendingClaims = pendingClaims.Count,
                TotalCoverage = policies.Sum(p => p.PremiumAmount) // Approximation
            },
            RecentPolicies = policies.Take(3),
            RecentClaims = claims.Take(3)
        };

        return Ok(result);
    }
}
