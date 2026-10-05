namespace SmartX.Shared.Commands;

/// <summary>
/// A manual override command targeting one device — the unit of work that
/// flows through the standard <see cref="DataStructures.CommandQueue"/>,
/// the <see cref="DataStructures.CriticalAlertQueue"/>, and the
/// <see cref="DataStructures.CommandHistoryStack"/> (for Undo).
/// </summary>
public sealed class DeviceCommand
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string SensorId { get; init; } = string.Empty;
    public CommandType Type { get; init; }
    public CommandSeverity Severity { get; init; } = CommandSeverity.Standard;
    public string? Payload { get; init; }
    public DateTimeOffset IssuedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// True when this command was generated automatically to reverse a prior
    /// one (see <see cref="DataStructures.CommandHistoryStack.Undo"/>) rather
    /// than issued directly by an operator — keeps the live feed and history
    /// honest about where a command actually came from.
    /// </summary>
    public bool IsUndo { get; init; }

    /// <summary>
    /// The command that, if issued immediately afterwards, reverses this
    /// one's effect. Used by the undo stack; a handful of command types
    /// (e.g. <see cref="CommandType.ResetSensor"/>) have no meaningful
    /// inverse and return <c>null</c>.
    /// </summary>
    public DeviceCommand? Inverse()
    {
        CommandType? inverseType = Type switch
        {
            CommandType.TriggerPump => CommandType.StopPump,
            CommandType.StopPump => CommandType.TriggerPump,
            CommandType.ToggleRelay => CommandType.ToggleRelay, // toggling twice restores state
            CommandType.OpenValve => CommandType.ShutValve,
            CommandType.ShutValve => CommandType.OpenValve,
            CommandType.ResetSensor => null,
            _ => null
        };

        if (inverseType is null) return null;

        return new DeviceCommand
        {
            SensorId = SensorId,
            Type = inverseType.Value,
            Severity = Severity,
            Payload = Payload,
            IsUndo = true
        };
    }

    public override string ToString() => $"{Type} -> {SensorId} ({Severity}) @ {IssuedUtc:O}";
}
