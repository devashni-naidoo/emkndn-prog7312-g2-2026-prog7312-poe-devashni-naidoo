namespace SmartX.Shared.Commands;

/// <summary>The outcome of processing a <see cref="DeviceCommand"/> off the queue.</summary>
public sealed class CommandResult
{
    public string CommandId { get; init; } = string.Empty;
    public string SensorId { get; init; } = string.Empty;
    public CommandType Type { get; init; }
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTimeOffset ExecutedUtc { get; init; } = DateTimeOffset.UtcNow;
    public bool WasCritical { get; init; }
}

/// <summary>Severity shown against a timeline / log entry.</summary>
public enum LogLevel
{
    Info,
    Warning,
    Critical
}

/// <summary>
/// One entry in a sensor's history log, keyed by timestamp in
/// <see cref="DataStructures.SensorLogTimeline"/> (a <c>SortedDictionary</c>)
/// so the dashboard can render an ordered timeline without re-sorting on
/// every request.
/// </summary>
public sealed class LogEntry
{
    public string SensorId { get; init; } = string.Empty;
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public LogLevel Level { get; init; } = LogLevel.Info;
    public string Message { get; init; } = string.Empty;
}
