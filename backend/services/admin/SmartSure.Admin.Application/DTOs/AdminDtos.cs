namespace SmartSure.Admin.Application.DTOs;

public class AdminClaimDto
{
    public int Id { get; set; }
    public int ClaimId { get; set; }
    public Guid UserId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public string ClaimNumber { get; set; } = string.Empty;
    public decimal ClaimAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime IncidentDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateClaimStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}

public class AdminUserDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime LastLogin { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminPolicyDto
{
    public int Id { get; set; }
    public Guid PolicyId { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string InsuranceType { get; set; } = string.Empty;
    public decimal PremiumAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class DashboardKpiDto
{
    public int TotalClaims { get; set; }
    public int PendingClaims { get; set; }
    public int ApprovedClaims { get; set; }
    public int TotalPolicies { get; set; }
    public decimal TotalRevenue { get; set; }
    public int ActiveUsers { get; set; }
}

public class ReportDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateReportRequest
{
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Parameters { get; set; } = string.Empty;
}

public class AuditLogDto
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
