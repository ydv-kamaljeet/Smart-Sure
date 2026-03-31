namespace SmartSure.Identity.Application.DTOs;

public class GoogleAuthSettings
{
    public const string SectionName = "GoogleAuth";
    public string ClientId { get; init; } = null!;
    public string ClientSecret { get; init; } = null!;
    public string RedirectUri { get; init; } = null!;
}
