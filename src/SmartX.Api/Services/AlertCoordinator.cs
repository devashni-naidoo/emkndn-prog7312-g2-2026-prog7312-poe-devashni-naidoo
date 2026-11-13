using Microsoft.AspNetCore.SignalR;
using SmartX.Api.Hubs;
using SmartX.Shared.Commands;
using SmartX.Shared.DataStructures;
using SmartX.Shared.Dtos;

namespace SmartX.Api.Services;


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


    /// Records that this command was issued for a sensor that currently has
    /// an active alert 
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
