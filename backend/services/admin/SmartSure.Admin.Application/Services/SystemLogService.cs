using System.Globalization;
using System.Text.RegularExpressions;
using SmartSure.Admin.Application.DTOs;
using SmartSure.Admin.Application.Interfaces;
using Microsoft.Extensions.Hosting;

namespace SmartSure.Admin.Application.Services;

public sealed partial class SystemLogService : ISystemLogService
{
    private static readonly Regex HeaderRegex = BuildHeaderRegex();
    private static readonly Regex UserRegex = new("UserId \\\"(?<userId>[^\\\"]+)\\\"", RegexOptions.Compiled);
    private static readonly Regex EventRegex = new("Received (?<eventName>[A-Za-z0-9]+Event)", RegexOptions.Compiled);

    private readonly string _baseServicesPath;

    public SystemLogService(IHostEnvironment environment)
    {
        // Walk up from the binary output dir to find the backend root
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        
        // Walk up until we find a directory that contains a "backend" subfolder
        // or until we find the "backend" directory itself
        while (current != null)
        {
            if (current.Name == "backend")
            {
                _baseServicesPath = current.FullName;
                break;
            }
            if (Directory.Exists(Path.Combine(current.FullName, "backend")))
            {
                _baseServicesPath = Path.Combine(current.FullName, "backend");
                break;
            }
            current = current.Parent;
        }

        _baseServicesPath ??= AppContext.BaseDirectory;
    }

    public async Task<PagedLogResultDto> SearchLogsAsync(LogSearchParametersDto parameters)
    {
        var allEntries = await ReadAllEntriesAsync(parameters.FromUtc, parameters.ToUtc);
        var query = allEntries.AsEnumerable();

        if (parameters.FromUtc.HasValue)
            query = query.Where(x => x.Timestamp >= parameters.FromUtc.Value);
        if (parameters.ToUtc.HasValue)
            query = query.Where(x => x.Timestamp <= parameters.ToUtc.Value);
        if (!string.IsNullOrWhiteSpace(parameters.Level))
            query = query.Where(x => x.Level.Equals(parameters.Level, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(parameters.Service))
            query = query.Where(x => x.Service.Equals(parameters.Service, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(parameters.UserId))
            query = query.Where(x => x.UserId != null && x.UserId.Equals(parameters.UserId, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(parameters.MessageContains))
            query = query.Where(x => x.RawEntry.Contains(parameters.MessageContains, StringComparison.OrdinalIgnoreCase));
        if (parameters.OnlyExceptions)
            query = query.Where(x => x.HasException || x.Level.Equals("ERR", StringComparison.OrdinalIgnoreCase));

        var ordered = query.OrderByDescending(x => x.Timestamp).ToList();
        var totalCount = ordered.Count;
        var page = Math.Max(1, parameters.Page);
        var pageSize = Math.Clamp(parameters.PageSize, 10, 200);
        var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedLogResultDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<LogMetadataDto> GetLogMetadataAsync()
    {
        // Only scan today's files for metadata to keep it fast
        var allEntries = await ReadAllEntriesAsync();
        return new LogMetadataDto
        {
            Services = allEntries.Select(x => x.Service).Distinct().OrderBy(x => x).ToList(),
            Levels = allEntries.Select(x => x.Level).Distinct().OrderBy(x => x).ToList()
        };
    }

    private async Task<List<LogEntryDto>> ReadAllEntriesAsync(DateTime? from = null, DateTime? to = null)
    {
        var entries = new List<LogEntryDto>();
        if (string.IsNullOrEmpty(_baseServicesPath) || !Directory.Exists(_baseServicesPath))
            return entries;

        var allLogFiles = Directory.GetFiles(_baseServicesPath, "log-*.txt", SearchOption.AllDirectories);

        // If no date filter, only read today's and yesterday's log files for performance
        // If date filter is set, read files that could contain logs in that range
        IEnumerable<string> logFiles;
        if (from == null && to == null)
        {
            var today = DateTime.Now.ToString("yyyyMMdd");
            var yesterday = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
            logFiles = allLogFiles.Where(f =>
            {
                var name = Path.GetFileNameWithoutExtension(f);
                return name.Contains(today) || name.Contains(yesterday);
            });
        }
        else
        {
            // Include files that fall within the date range
            // Use local date since log filenames are based on local time
            var startDate = (from ?? DateTime.Now.AddDays(-7)).ToLocalTime().Date;
            var endDate = (to ?? DateTime.Now).ToLocalTime().Date;
            logFiles = allLogFiles.Where(f =>
            {
                var name = Path.GetFileNameWithoutExtension(f);
                var datePart = name.Replace("log-", "").Split('_')[0];
                if (DateTime.TryParseExact(datePart, "yyyyMMdd", null, DateTimeStyles.None, out var fileDate))
                    return fileDate.Date >= startDate && fileDate.Date <= endDate;
                return false;
            });
        }

        foreach (var file in logFiles)
        {
            try
            {
                // Use FileShare.Read so we don't conflict with Serilog's write lock
                using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();
                var lines = content.Split(Environment.NewLine);
                var currentEntryLines = new List<string>();

                foreach (var line in lines)
                {
                    if (HeaderRegex.IsMatch(line) && currentEntryLines.Count > 0)
                    {
                        entries.Add(ParseEntry(currentEntryLines));
                        currentEntryLines.Clear();
                    }
                    currentEntryLines.Add(line);
                }
                if (currentEntryLines.Count > 0)
                    entries.Add(ParseEntry(currentEntryLines));
            }
            catch (IOException) { continue; }
        }

        return entries;
    }

    private static LogEntryDto ParseEntry(List<string> lines)
    {
        var firstLine = lines[0];
        var match = HeaderRegex.Match(firstLine);

        if (!match.Success)
        {
            return new LogEntryDto
            {
                Timestamp = DateTimeOffset.MinValue,
                Level = "UNK",
                Service = "Unknown",
                Message = string.Join(Environment.NewLine, lines),
                RawEntry = string.Join(Environment.NewLine, lines),
                HasException = string.Join(Environment.NewLine, lines).Contains("Exception")
            };
        }

        DateTimeOffset timestamp;
        if (!DateTimeOffset.TryParseExact(
            match.Groups["timestamp"].Value,
            "yyyy-MM-dd HH:mm:ss.fff zzz",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out timestamp))
        {
             timestamp = DateTimeOffset.MinValue;
        }

        var message = match.Groups["message"].Value;
        if (lines.Count > 1)
        {
            message = string.Join(Environment.NewLine, new[] { message }.Concat(lines.Skip(1)));
        }

        var userMatch = UserRegex.Match(message);
        var eventMatch = EventRegex.Match(message);

        return new LogEntryDto
        {
            Timestamp = timestamp,
            Level = match.Groups["level"].Value,
            Service = match.Groups["service"].Value,
            Message = message,
            RawEntry = string.Join(Environment.NewLine, lines),
            UserId = userMatch.Success ? userMatch.Groups["userId"].Value : null,
            EventName = eventMatch.Success ? eventMatch.Groups["eventName"].Value : null,
            HasException = message.Contains("Exception:") || message.Contains("Stack trace:")
        };
    }

    [GeneratedRegex("^(?<timestamp>\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2}\\.\\d{3} [+-]\\d{2}:\\d{2}) \\[(?<level>[A-Z]{3})\\] \\[(?<service>[^\\]]+)\\] (?<message>.*)$", RegexOptions.Compiled)]
    private static partial Regex BuildHeaderRegex();
}
