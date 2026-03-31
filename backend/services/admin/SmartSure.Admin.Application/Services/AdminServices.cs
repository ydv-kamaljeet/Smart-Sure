using SmartSure.Admin.Application.DTOs;
using SmartSure.Admin.Application.Interfaces;
using SmartSure.Admin.Domain.Entities;
using SmartSure.Shared.Common.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace SmartSure.Admin.Application.Services;

public class AdminUsersService : IAdminUsersService
{
    private readonly IAdminRepository<AdminUser> _userRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAdminAuditLogService _auditLogService;

    public AdminUsersService(IAdminRepository<AdminUser> userRepo, IUnitOfWork unitOfWork, IAdminAuditLogService auditLogService)
    {
        _userRepo = userRepo;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
    }

    public async Task<PagedResult<AdminUserDto>> GetUsersAsync(string? searchTerm, string? role, int page, int pageSize)
    {
        var allUsers = await _userRepo.GetAllAsync();
        var query = allUsers.AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm)) 
            query = query.Where(u => u.FullName.Contains(searchTerm) || u.Email.Contains(searchTerm));
        if (!string.IsNullOrEmpty(role)) 
            query = query.Where(u => u.Role == role);

        var total = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize).Select(MapToDto).ToList();

        return new PagedResult<AdminUserDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AdminUserDto?> GetUserByIdAsync(Guid userId)
    {
        var allUsers = await _userRepo.GetAllAsync();
        var user = allUsers.FirstOrDefault(u => u.UserId == userId);
        return user != null ? MapToDto(user) : null;
    }

    public async Task<bool> SoftDeleteUserAsync(Guid userId)
    {
        var allUsers = await _userRepo.GetAllAsync();
        var user = allUsers.FirstOrDefault(u => u.UserId == userId);
        if (user == null) return false;

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
        
        await _auditLogService.LogActionAsync("Deactivate User", "AdminUser", userId.ToString(), $"User {user.Email} deactivated.");
        
        // TODO: Publish UserDeactivated event
        return true;
    }

    private AdminUserDto MapToDto(AdminUser user) => new AdminUserDto
    {
        Id = user.Id,
        UserId = user.UserId.ToString(),
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role,
        IsActive = user.IsActive,
        LastLogin = user.LastLogin,
        CreatedAt = user.CreatedAt
    };
}

public class AdminPolicyService : IAdminPolicyService
{
    private readonly IAdminRepository<AdminPolicy> _policyRepo;

    public AdminPolicyService(IAdminRepository<AdminPolicy> policyRepo)
    {
        _policyRepo = policyRepo;
    }

    public async Task<PagedResult<AdminPolicyDto>> GetPoliciesAsync(string? searchTerm, string? status, int page, int pageSize)
    {
        var allPolicies = await _policyRepo.GetAllAsync();
        var query = allPolicies.AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(p => p.PolicyNumber.Contains(searchTerm) || p.CustomerName.Contains(searchTerm));

        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status.Equals(status, StringComparison.OrdinalIgnoreCase));

        var total = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize).Select(MapToDto).ToList();

        return new PagedResult<AdminPolicyDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    private AdminPolicyDto MapToDto(AdminPolicy policy) => new AdminPolicyDto
    {
        Id = policy.Id,
        PolicyId = policy.PolicyId,
        PolicyNumber = policy.PolicyNumber,
        CustomerName = policy.CustomerName,
        InsuranceType = policy.InsuranceType,
        PremiumAmount = policy.PremiumAmount,
        Status = policy.Status
    };
}

public class AdminDashboardService : IAdminDashboardService
{
    private readonly IAdminRepository<AdminClaim> _claimRepo;
    private readonly IAdminRepository<AdminPolicy> _policyRepo;
    private readonly IAdminRepository<AdminUser> _userRepo;

    public AdminDashboardService(IAdminRepository<AdminClaim> claimRepo, IAdminRepository<AdminPolicy> policyRepo, IAdminRepository<AdminUser> userRepo)
    {
        _claimRepo = claimRepo;
        _policyRepo = policyRepo;
        _userRepo = userRepo;
    }

    public async Task<DashboardKpiDto> GetDashboardKpisAsync()
    {
        // Redis caching logic would go here
        var claims = await _claimRepo.GetAllAsync();
        var policies = await _policyRepo.GetAllAsync();
        var users = await _userRepo.GetAllAsync();

        return new DashboardKpiDto
        {
            TotalClaims = claims.Count(),
            PendingClaims = claims.Count(c => c.Status == "Submitted"),
            ApprovedClaims = claims.Count(c => c.Status == "Approved"),
            TotalPolicies = policies.Count(),
            TotalRevenue = policies.Sum(p => p.PremiumAmount),
            ActiveUsers = users.Count(u => u.IsActive)
        };
    }
}

public class AdminReportsService : IAdminReportsService
{
    private readonly IAdminRepository<Report> _reportRepo;
    private readonly IAdminRepository<AdminClaim> _claimRepo;
    private readonly IAdminRepository<AdminPolicy> _policyRepo;
    private readonly IAdminRepository<AdminUser> _userRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAdminAuditLogService _auditLogService;

    public AdminReportsService(
        IAdminRepository<Report> reportRepo,
        IAdminRepository<AdminClaim> claimRepo,
        IAdminRepository<AdminPolicy> policyRepo,
        IAdminRepository<AdminUser> userRepo,
        IUnitOfWork unitOfWork,
        IAdminAuditLogService auditLogService)
    {
        _reportRepo = reportRepo;
        _claimRepo = claimRepo;
        _policyRepo = policyRepo;
        _userRepo = userRepo;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
    }

    public async Task<PagedResult<ReportDto>> GetReportsAsync(int page, int pageSize)
    {
        var allReports = await _reportRepo.GetAllAsync();
        var total = allReports.Count();
        var items = allReports.OrderByDescending(r => r.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).Select(MapToDto).ToList();

        return new PagedResult<ReportDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ReportDto> TriggerReportGenerationAsync(CreateReportRequest request)
    {
        var report = new Report
        {
            Title = request.Title,
            Type = request.Type,
            Parameters = request.Parameters,
            Status = "Pending",
            FileUrl = "",
            CreatedAt = DateTime.UtcNow
        };

        await _reportRepo.AddAsync(report);
        await _unitOfWork.SaveChangesAsync();

        try
        {
            var csv = await GenerateCsvAsync(request.Type);
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            var base64 = Convert.ToBase64String(bytes);
            report.FileUrl = $"data:text/csv;base64,{base64}";
            report.Status = "Completed";
        }
        catch
        {
            report.Status = "Failed";
        }

        await _reportRepo.UpdateAsync(report);
        await _unitOfWork.SaveChangesAsync();

        await _auditLogService.LogActionAsync("Trigger Report", "Report", report.Id.ToString(), $"Report '{report.Title}' generated.");

        return MapToDto(report);
    }

    private async Task<string> GenerateCsvAsync(string type)
    {
        var sb = new System.Text.StringBuilder();

        switch (type)
        {
            case "Claims":
                var claims = await _claimRepo.GetAllAsync();
                sb.AppendLine("ClaimNumber,CustomerName,PolicyNumber,Amount,Status,IncidentDate,CreatedAt");
                foreach (var c in claims)
                    sb.AppendLine($"{c.ClaimNumber},{Escape(c.CustomerName)},{c.PolicyNumber},{c.ClaimAmount},{c.Status},{c.IncidentDate:yyyy-MM-dd},{c.CreatedAt:yyyy-MM-dd}");
                break;

            case "Policies":
                var policies = await _policyRepo.GetAllAsync();
                sb.AppendLine("PolicyNumber,CustomerName,InsuranceType,PremiumAmount,Status");
                foreach (var p in policies)
                    sb.AppendLine($"{p.PolicyNumber},{Escape(p.CustomerName)},{p.InsuranceType},{p.PremiumAmount},{p.Status}");
                break;

            case "Revenue":
                var revPolicies = await _policyRepo.GetAllAsync();
                var totalRevenue = revPolicies.Sum(p => p.PremiumAmount);
                var byType = revPolicies.GroupBy(p => p.InsuranceType)
                    .Select(g => new { Type = g.Key, Revenue = g.Sum(p => p.PremiumAmount), Count = g.Count() });
                sb.AppendLine("InsuranceType,PolicyCount,TotalRevenue");
                foreach (var r in byType)
                    sb.AppendLine($"{r.Type},{r.Count},{r.Revenue}");
                sb.AppendLine($"TOTAL,{revPolicies.Count()},{totalRevenue}");
                break;

            case "Users":
                var users = await _userRepo.GetAllAsync();
                sb.AppendLine("FullName,Email,Role,IsActive,LastLogin,CreatedAt");
                foreach (var u in users)
                    sb.AppendLine($"{Escape(u.FullName)},{u.Email},{u.Role},{u.IsActive},{u.LastLogin:yyyy-MM-dd},{u.CreatedAt:yyyy-MM-dd}");
                break;

            default:
                sb.AppendLine("No data available for this report type.");
                break;
        }

        return sb.ToString();
    }

    private static string Escape(string val) => val.Contains(',') ? $"\"{val}\"" : val;

    public async Task<ReportDto?> GetReportByIdAsync(Guid reportId)
    {
        var report = await _reportRepo.GetByIdAsync(reportId);
        return report != null ? MapToDto(report) : null;
    }

    private ReportDto MapToDto(Report report) => new ReportDto
    {
        Id = report.Id,
        Title = report.Title,
        Type = report.Type,
        FileUrl = report.FileUrl,
        Status = report.Status,
        CreatedAt = report.CreatedAt
    };
}

public class AdminAuditLogService : IAdminAuditLogService
{
    private readonly IAdminRepository<AuditLog> _auditLogRepo;
    private readonly IAdminRepository<AdminUser> _userRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public AdminAuditLogService(IAdminRepository<AuditLog> auditLogRepo, IAdminRepository<AdminUser> userRepo, IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
    {
        _auditLogRepo = auditLogRepo;
        _userRepo = userRepo;
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(string? entityType, Guid? userId, DateTime? fromDate, int page, int pageSize)
    {
        var allLogs = await _auditLogRepo.GetAllAsync();
        var query = allLogs.AsQueryable();

        if (!string.IsNullOrEmpty(entityType)) query = query.Where(l => l.EntityName == entityType);
        if (userId.HasValue) query = query.Where(l => l.UserId == userId.Value);
        if (fromDate.HasValue) query = query.Where(l => l.CreatedAt >= fromDate.Value);

        var total = query.Count();
        var logs = query.OrderByDescending(l => l.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var allUsers = await _userRepo.GetAllAsync();
        var userMap = allUsers.ToDictionary(u => u.UserId, u => u.FullName);

        var items = logs.Select(l => MapToDto(l, userMap)).ToList();

        return new PagedResult<AuditLogDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task CreateAuditLogAsync(AuditLogDto auditLog)
    {
        var log = new AuditLog
        {
            UserId = auditLog.UserId,
            Action = auditLog.Action,
            EntityName = auditLog.EntityName,
            EntityId = auditLog.EntityId,
            Details = auditLog.Details,
            IpAddress = auditLog.IpAddress,
            CreatedAt = DateTime.UtcNow
        };
        await _auditLogRepo.AddAsync(log);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task LogActionAsync(string action, string entityName, string entityId, string details)
    {
        var userIdStr = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid.TryParse(userIdStr, out var userId);
        var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";

        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };
        await _auditLogRepo.AddAsync(log);
        await _unitOfWork.SaveChangesAsync();
    }

    private AuditLogDto MapToDto(AuditLog log, Dictionary<Guid, string> userMap) => new AuditLogDto
    {
        Id = log.Id,
        UserId = log.UserId,
        UserName = userMap.TryGetValue(log.UserId, out var name) ? name : "System",
        Action = log.Action,
        EntityName = log.EntityName,
        EntityId = log.EntityId,
        Details = log.Details,
        IpAddress = log.IpAddress,
        Timestamp = DateTime.SpecifyKind(log.CreatedAt, DateTimeKind.Utc)
    };
}
