using System.Collections.Concurrent;
using SmartX.Shared.Commands;

namespace SmartX.Api.Services;

/// <summary>
/// Tracks the simulated physical state a manual override command actually
/// changes (relay on/off, valve open/shut, pump running) so toggling a
/// device from the dashboard has an observable, queryable effect rather
/// than just logging "command sent" into the void.
/// </summary>
public sealed class DeviceStateService
{
    private readonly ConcurrentDictionary<string, bool> _relayOn = new();
    private readonly ConcurrentDictionary<string, bool> _valveOpen = new();
    private readonly ConcurrentDictionary<string, bool> _pumpRunning = new();

    public bool IsRelayOn(string sensorId) => _relayOn.GetValueOrDefault(sensorId, false);
    public bool IsValveOpen(string sensorId) => _valveOpen.GetValueOrDefault(sensorId, true);
    public bool IsPumpRunning(string sensorId) => _pumpRunning.GetValueOrDefault(sensorId, false);

    /// <summary>Applies a command's effect and returns a human-readable outcome message.</summary>
    public string Apply(DeviceCommand command)
    {
        switch (command.Type)
        {
            case CommandType.ToggleRelay:
                var newState = !IsRelayOn(command.SensorId);
                _relayOn[command.SensorId] = newState;
                return $"Relay {(newState ? "ON" : "OFF")}";

            case CommandType.OpenValve:
                _valveOpen[command.SensorId] = true;
                return "Valve OPEN";

            case CommandType.ShutValve:
                _valveOpen[command.SensorId] = false;
                return "Valve SHUT";

            case CommandType.TriggerPump:
                _pumpRunning[command.SensorId] = true;
                return "Pump RUNNING";

            case CommandType.StopPump:
                _pumpRunning[command.SensorId] = false;
                return "Pump STOPPED";

            case CommandType.ResetSensor:
                return "Sensor reset acknowledged";

            default:
                return "No-op";
        }
    }
}
