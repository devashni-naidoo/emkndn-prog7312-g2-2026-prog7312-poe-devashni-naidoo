using Microsoft.AspNetCore.SignalR;
using SmartX.Api.Hubs;
using SmartX.Shared.Commands;
using SmartX.Shared.DataStructures;
using SmartX.Shared.Dtos;

namespace SmartX.Api.Services;

/// <summary>
/// Central point where Part 1's anomaly engine (<see cref="TelemetryStore"/>)
/// and Part 2's command stream meet: owns the <see cref="ActiveAlertSet"/>
/// (so a still-active alert is never re-raised or re-broadcast), the
/// <see cref="SensorLogTimeline"/> (so every alert/resolution/command has a
/// timestamped history entry), and the <see cref="PatternFrequencyTracker"/>
/// behind the predictive insights engine.
/// </summary>
public sealed class AlertCoordinator
{
    private readonly ActiveAlertSet _activeAlerts = new();
    private readonly SensorLogTimeline _timeline = new();
    private readonly PatternFrequencyTracker _patterns = new();
    private readonly IHubContext<CommandHub> _hub;
    private readonly ILogger<AlertCoordinator> _logger;

    public AlertCoordinator(IHubContext<CommandHub> hub, ILogger<AlertCoordinator> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public ActiveAlertSet ActiveAlerts => _activeAlerts;
    public SensorLogTimeline Timeline => _timeline;
    public PatternFrequencyTracker Patterns => _patterns;

    /// <summary>
    /// Called by <see cref="TelemetryStore"/> whenever a sensor's computed
    /// status changes. Only broadcasts/logs on an actual state transition —
    /// the <see cref="ActiveAlertSet"/>'s set semantics are what make that
    /// idempotent even if this is called on every single poll.
    /// </summary>
    public async Task SyncAlertStateAsync(string sensorId, string displayName, string alertType, bool shouldBeActive)
    {
        string key = ActiveAlertSet.KeyFor(sensorId, alertType);

        if (shouldBeActive)
        {
            if (_activeAlerts.Raise(key))
            {
                _timeline.Append(new LogEntry
                {
                    SensorId = sensorId,
                    Level = SmartX.Shared.Commands.LogLevel.Warning,
                    Message = $"{alertType} alert raised on {displayName}"
                });
                await _hub.Clients.All.SendAsync("ReceiveAlert", new
                {
                    sensorId,
                    displayName,
                    alertType,
                    active = true,
                    atUtc = DateTimeOffset.UtcNow
                });
            }
        }
        else
        {
            if (_activeAlerts.Clear(key))
            {
                _timeline.Append(new LogEntry
                {
                    SensorId = sensorId,
                    Level = SmartX.Shared.Commands.LogLevel.Info,
                    Message = $"{alertType} alert cleared on {displayName}"
                });
                await _hub.Clients.All.SendAsync("ReceiveAlert", new
                {
                    sensorId,
                    displayName,
                    alertType,
                    active = false,
                    atUtc = DateTimeOffset.UtcNow
                });
            }
        }
    }

    /// <summary>
    /// Records that this command was issued for a sensor that currently has
    /// an active alert — the raw material the predictive engine's frequency
    /// counter learns from.
    /// </summary>
    public void RecordCommandAgainstActiveAlerts(string sensorId, CommandType command)
    {
        foreach (var alertType in new[] { "Anomalous", "Disconnected" })
        {
            string key = ActiveAlertSet.KeyFor(sensorId, alertType);
            if (_activeAlerts.IsActive(key))
            {
                _patterns.RecordCommandDuringAlert(alertType, command);
            }
        }
    }

    public void RecordSearch(string term) => _patterns.RecordSearch(term);

    public async Task LogAsync(string sensorId, Shared.Commands.LogLevel level, string message)
    {
        _timeline.Append(new LogEntry { SensorId = sensorId, Level = level, Message = message });
        _logger.LogInformation("[{Level}] {SensorId}: {Message}", level, sensorId, message);
        await Task.CompletedTask;
    }
}
