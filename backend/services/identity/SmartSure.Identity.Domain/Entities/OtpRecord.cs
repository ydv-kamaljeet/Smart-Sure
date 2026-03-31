namespace SmartSure.Identity.Domain.Entities;

public class OtpRecord
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string HashedOtp { get; set; } = string.Empty;
    public DateTime Expiry { get; set; }
    public int Attempts { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
