using System;
using SmartSure.Shared.Common.Models;

namespace SmartSure.Claims.Domain.Entities;

public class ValidPolicy : BaseEntity
{
    public Guid PolicyId { get; set; }
    public Guid UserId { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public decimal InsuredDeclaredValue { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
