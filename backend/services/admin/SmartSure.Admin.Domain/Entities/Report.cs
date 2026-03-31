using SmartSure.Shared.Common.Models;

namespace SmartSure.Admin.Domain.Entities;

public class Report : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // e.g., "Monthly Claims", "User Growth"
    public string FileUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Completed, Failed
    public string Parameters { get; set; } = string.Empty; // JSON of filters used
}
