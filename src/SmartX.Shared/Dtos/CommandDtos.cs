using SmartX.Shared.Commands;

namespace SmartX.Shared.Dtos;

public sealed class IssueCommandRequest
{
    public string SensorId { get; set; } = string.Empty;
    public CommandType Type { get; set; }
    public CommandSeverity Severity { get; set; } = CommandSeverity.Standard;
    public string? Payload { get; set; }
}

public sealed class DeviceSearchResultDto
{
    public string SensorId { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public Models.SensorCategory Category { get; set; }
    public TelemetryStatus TelemetryStatus { get; set; }
    public bool HasActiveAlert { get; set; }
    public IReadOnlyList<string> ActiveAlertKeys { get; set; } = Array.Empty<string>();
}

public sealed class SuggestedActionDto
{
    public string SensorId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public CommandType SuggestedCommand { get; set; }
    public int Occurrences { get; set; }
    public string Rationale { get; set; } = string.Empty;
}

public sealed class LogEntryDto
{
    public string SensorId { get; set; } = string.Empty;
    public DateTimeOffset TimestampUtc { get; set; }
    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class CommandResultDto
{
    public string CommandId { get; set; } = string.Empty;
    public string SensorId { get; set; } = string.Empty;
    public CommandType Type { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset ExecutedUtc { get; set; }
    public bool WasCritical { get; set; }
}
