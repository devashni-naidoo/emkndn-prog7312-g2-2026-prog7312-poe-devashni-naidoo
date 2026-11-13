using System.Collections.Concurrent;
using SmartX.Shared.Models;

namespace SmartX.Api.Services;

/// Generates realistic simulated telemetry for a registered sensor so the
/// dashboard's real-time engagement strategy 
public sealed class SensorSimulator
{
    private readonly TelemetryStore _store;
    private readonly SensorRegistry _registry;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _running = new();
    private readonly Random _random = new();

    public SensorSimulator(TelemetryStore store, SensorRegistry registry)
    {
        _store = store;
        _registry = registry;
    }

    public bool IsRunning(string sensorId) => _running.ContainsKey(sensorId);

    public bool Start(string sensorId)
    {
        var profile = _registry.Get(sensorId);
        if (profile is null) return false;
        if (_running.ContainsKey(sensorId)) return true;

        var cts = new CancellationTokenSource();
        if (!_running.TryAdd(sensorId, cts)) return true;

        _ = RunLoopAsync(sensorId, profile.Category, cts.Token);
        return true;
    }

    public void Stop(string sensorId)
    {
        if (_running.TryRemove(sensorId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    private async Task RunLoopAsync(string sensorId, SensorCategory category, CancellationToken token)
    {
        double baseline = category switch
        {
            SensorCategory.Environmental => 22.0,
            SensorCategory.PowerConsumption => 120.0,
            SensorCategory.Actuator => 1.0,
            _ => 10.0
        };

        int tick = 0;
        try
        {
            while (!token.IsCancellationRequested)
            {
                tick++;

                // Every 9th tick injects a deliberate anomalous spike so the
                // threshold-based highlighting strategy has something to catch.
                bool spike = tick % 9 == 0;
                double value = spike
                    ? baseline * (1 + _random.NextDouble() * 1.5 + 0.8)
                    : baseline + (_random.NextDouble() - 0.5) * baseline * 0.06;

                await _store.IngestAsync(sensorId, Math.Round(value, 2));

                await Task.Delay(TimeSpan.FromSeconds(2), token);
            }
        }
        catch (TaskCanceledException)
        {
            // Expected on Stop().
        }
    }
}
