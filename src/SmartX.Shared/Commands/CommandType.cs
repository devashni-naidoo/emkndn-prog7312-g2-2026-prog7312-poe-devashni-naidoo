namespace SmartX.Shared.Commands;

/// <summary>Manual override commands an operator can issue to a device.</summary>
public enum CommandType
{
    TriggerPump,
    StopPump,
    ToggleRelay,
    OpenValve,
    ShutValve,
    ResetSensor
}

/// <summary>
/// Routes a command to the standard FIFO queue or the priority queue. A
/// severe power spike or a critical moisture drop must bypass the standard
/// queue and be processed immediately — that's what <see cref="Critical"/>
/// is for.
/// </summary>
public enum CommandSeverity
{
    Standard,
    Critical
}
