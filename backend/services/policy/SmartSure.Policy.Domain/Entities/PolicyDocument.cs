namespace SmartSure.Policy.Domain.Entities;

public class PolicyDocument
{
    public Guid Id { get; set; }
    public Guid PolicyId { get; set; }
    
    public string DocumentUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public long FileSize { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Policy? Policy { get; set; }
}
