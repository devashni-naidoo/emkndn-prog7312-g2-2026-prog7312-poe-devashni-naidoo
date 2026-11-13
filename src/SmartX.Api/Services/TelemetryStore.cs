using System.Collections.Concurrent;
using SmartX.Shared.DataStructures;
using SmartX.Shared.Dtos;
using SmartX.Shared.Models;

namespace SmartX.Api.Services;


/// Ingests readings (singly or as jagged-array batches), keeps a rolling
/// sparkline window per sensor, and computes the threshold-based anomaly
/// status that drives the dashboard's real-time visual telemetry feedback 
public sealed class TelemetryStore
{
    private readonly ConcurrentDictionary<string, SensorTelemetryState> _state = new();
    private readonly SensorRegistry _registry;
    private readonly AlertCoordinator _alerts;
    private readonly TelemetryHistoryIndex _history;

    private const int SparklineWindow = 20;
    private static readonly TimeSpan DisconnectAfter = TimeSpan.FromSeconds(15);

    public TelemetryStore(SensorRegistry registry, AlertCoordinator alerts, TelemetryHistoryIndex history)
    {
        _registry = registry;
        _alerts = alerts;
        _history = history;
    }


    /// The timestamped AVL-tree history behind the Part 3 range queries.
    /// Every reading ingested below is also appended here.

    public TelemetryHistoryIndex History => _history;


    public async Task<TelemetrySnapshotDto> IngestAsync(string sensorId, double value)
    {
        var state = _state.GetOrAdd(sensorId, id => new SensorTelemetryState(id));
        lock (state)
        {
            RecordValue(state, value);
            state.Buffer.AddBatch(new[] { value });
            state.LastUpdatedUtc = DateTimeOffset.UtcNow;
            _history.Append(sensorId, state.LastUpdatedUtc, value);
        }
        return await BuildSnapshotAsync(sensorId, state);
    }


    /// Ingests a burst of readings as a single jagged-array batch — e.g. a
    /// sensor flushing buffered samples after reconnecting.

    public int IngestBatch(string sensorId, double[] values)
    {
        if (values.Length == 0) return 0;

        var state = _state.GetOrAdd(sensorId, id => new SensorTelemetryState(id));
        lock (state)
        {
            state.Buffer.AddBatch(values);
            var now = DateTimeOffset.UtcNow;
            foreach (var v in values)
            {
                RecordValue(state, v);
                _history.Append(sensorId, now, v);
            }
            state.LastUpdatedUtc = now;
        }
        return values.Length;
    }

    private static void RecordValue(SensorTelemetryState state, double value)
    {
        state.PreviousValue = state.RecentValues.Count > 0 ? state.LastValue : value;
        state.LastValue = value;
        state.RecentValues.Add(value);
        if (state.RecentValues.Count > SparklineWindow)
        {
            state.RecentValues.RemoveAt(0);
        }
    }

    public async Task<TelemetrySnapshotDto?> GetSnapshotAsync(string sensorId)
    {
        return _state.TryGetValue(sensorId, out var state) ? await BuildSnapshotAsync(sensorId, state) : null;
    }

    public async Task<IReadOnlyList<TelemetrySnapshotDto>> GetAllSnapshotsAsync()
    {
        var results = new List<TelemetrySnapshotDto>();
        foreach (var kvp in _state)
        {
            results.Add(await BuildSnapshotAsync(kvp.Key, kvp.Value));
        }
        return results;
    }

    public double[][]? GetRawBatches(string sensorId) =>
        _state.TryGetValue(sensorId, out var state) ? state.Buffer.SnapshotBatches() : null;

    private async Task<TelemetrySnapshotDto> BuildSnapshotAsync(string sensorId, SensorTelemetryState state)
    {
        var profile = _registry.Get(sensorId);

        double delta = state.PreviousValue == 0
            ? 0
            : (state.LastValue - state.PreviousValue) / Math.Abs(state.PreviousValue) * 100.0;

        double secondsSince = (DateTimeOffset.UtcNow - state.LastUpdatedUtc).TotalSeconds;
        double threshold = profile?.AnomalyThresholdPercent ?? 30.0;

        TelemetryStatus status;
        if (secondsSince > DisconnectAfter.TotalSeconds)
        {
            status = TelemetryStatus.Disconnected;
        }
        else if (Math.Abs(delta) >= threshold)
        {
            status = TelemetryStatus.Anomalous;
        }
        else if (Math.Abs(delta) >= threshold * 0.6)
        {
            status = TelemetryStatus.Warning;
        }
        else
        {
            status = TelemetryStatus.Normal;
        }

        string displayName = profile is not null ? $"{profile.Location.NodeId} · {profile.Category}" : sensorId;

   
        await _alerts.SyncAlertStateAsync(sensorId, displayName, "Anomalous", status == TelemetryStatus.Anomalous);
        await _alerts.SyncAlertStateAsync(sensorId, displayName, "Disconnected", status == TelemetryStatus.Disconnected);

        return new TelemetrySnapshotDto
        {
            SensorId = sensorId,
            DisplayName = displayName,
            Zone = profile?.Location.Zone ?? "Unassigned",
            Unit = UnitFor(profile?.Category),
            Category = profile?.Category ?? SensorCategory.Environmental,
            LatestValue = Math.Round(state.LastValue, 2),
            DeltaPercent = Math.Round(delta, 1),
            Status = status,
            Sparkline = state.RecentValues.ToArray(),
            LastUpdatedUtc = state.LastUpdatedUtc,
            SecondsSinceUpdate = Math.Round(secondsSince, 1)
        };
    }

    private static string UnitFor(SensorCategory? category) => category switch
    {
        SensorCategory.Environmental => "°C",
        SensorCategory.PowerConsumption => "W",
        SensorCategory.Actuator => "",
        _ => ""
    };
}
