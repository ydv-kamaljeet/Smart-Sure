namespace SmartSure.Admin.Application.DTOs;

public class LogEntryDto
{
    public DateTimeOffset Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string RawEntry { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? EventName { get; set; }
    public bool HasException { get; set; }
}

public class LogSearchParametersDto
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Level { get; set; }
    public string? Service { get; set; }
    public string? MessageContains { get; set; }
    public string? UserId { get; set; }
    public bool OnlyExceptions { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;

    // Normalized dates for file filtering — keep as local time to match log filenames
    public DateTime? FromUtc => From;
    public DateTime? ToUtc => To;
}

public class PagedLogResultDto
{
    public IEnumerable<LogEntryDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class LogMetadataDto
{
    public List<string> Services { get; set; } = new();
    public List<string> Levels { get; set; } = new();
}
