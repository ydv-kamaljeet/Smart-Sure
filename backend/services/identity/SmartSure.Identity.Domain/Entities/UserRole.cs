namespace SmartSure.Identity.Domain.Entities;

public class UserRole
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public int RoleId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public Role? Role { get; set; }
}
