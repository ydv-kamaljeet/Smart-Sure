using SmartSure.Shared.Common.Models;

namespace SmartSure.Admin.Domain.Entities;

public class AdminUser : BaseEntity
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime LastLogin { get; set; }
}
