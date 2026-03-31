namespace SmartSure.Identity.Domain.Entities;

public class Password
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime LastChangedAt { get; set; } = DateTime.UtcNow;
    public bool MustChangePassword { get; set; } = false;

    public User? User { get; set; }
}
