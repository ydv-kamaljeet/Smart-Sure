using SmartSure.Admin.Application.DTOs;
using SmartSure.Shared.Common.Models;

namespace SmartSure.Admin.Application.Interfaces;

public interface IAdminClaimsService
{
    Task<PagedResult<AdminClaimDto>> GetClaimsAsync(string? status, DateTime? fromDate, Guid? userId, int page, int pageSize);
    Task<AdminClaimDto?> GetClaimByIdAsync(int claimId);
    Task<bool> MarkAsUnderReviewAsync(int claimId, string remarks);
    Task<bool> ApproveClaimAsync(int claimId, string remarks);
    Task<bool> RejectClaimAsync(int claimId, string reason);
}

public interface IAdminUsersService
{
    Task<PagedResult<AdminUserDto>> GetUsersAsync(string? searchTerm, string? role, int page, int pageSize);
    Task<AdminUserDto?> GetUserByIdAsync(Guid userId);
    Task<bool> SoftDeleteUserAsync(Guid userId);
}

public interface IAdminPolicyService
{
    Task<PagedResult<AdminPolicyDto>> GetPoliciesAsync(string? searchTerm, string? status, int page, int pageSize);
}

public interface IAdminDashboardService
{
    Task<DashboardKpiDto> GetDashboardKpisAsync();
}

public interface IAdminReportsService
{
    Task<PagedResult<ReportDto>> GetReportsAsync(int page, int pageSize);
    Task<ReportDto> TriggerReportGenerationAsync(CreateReportRequest request);
    Task<ReportDto?> GetReportByIdAsync(Guid reportId);
}

public interface IAdminAuditLogService
{
    Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(string? entityType, Guid? userId, DateTime? fromDate, int page, int pageSize);
    Task CreateAuditLogAsync(AuditLogDto auditLog);
    Task LogActionAsync(string action, string entityName, string entityId, string details);
}
