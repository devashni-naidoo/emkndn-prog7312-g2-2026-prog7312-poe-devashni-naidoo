using System.Collections.Concurrent;
using SmartX.Shared.Dtos;
using SmartX.Shared.Models;

namespace SmartX.Api.Services;

/// <summary>
/// Ingests readings (singly or as jagged-array batches), keeps a rolling
/// sparkline window per sensor, and computes the threshold-based anomaly
/// status that drives the dashboard's real-time visual telemetry feedback —
/// the engagement strategy selected in the Task 1 research report.
/// </summary>
public sealed class TelemetryStore
{
    private readonly ConcurrentDictionary<string, SensorTelemetryState> _state = new();
    private readonly SensorRegistry _registry;

    private const int SparklineWindow = 20;
    private static readonly TimeSpan DisconnectAfter = TimeSpan.FromSeconds(15);

    public TelemetryStore(SensorRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>Ingests a single reading.</summary>
    public TelemetrySnapshotDto Ingest(string sensorId, double value)
    {
        var state = _state.GetOrAdd(sensorId, id => new SensorTelemetryState(id));
        lock (state)
        {
            RecordValue(state, value);
            state.Buffer.AddBatch(new[] { value });
            state.LastUpdatedUtc = DateTimeOffset.UtcNow;
        }
        return BuildSnapshot(sensorId, state);
    }

    /// <summary>
    /// Ingests a burst of readings as a single jagged-array batch — e.g. a
    /// sensor flushing buffered samples after reconnecting.
    /// </summary>
    public int IngestBatch(string sensorId, double[] values)
    {
        if (values.Length == 0) return 0;

        var state = _state.GetOrAdd(sensorId, id => new SensorTelemetryState(id));
        lock (state)
        {
            state.Buffer.AddBatch(values);
            foreach (var v in values)
            {
                RecordValue(state, v);
            }
            state.LastUpdatedUtc = DateTimeOffset.UtcNow;
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

    public TelemetrySnapshotDto? GetSnapshot(string sensorId)
    {
        return _state.TryGetValue(sensorId, out var state) ? BuildSnapshot(sensorId, state) : null;
    }

    public IReadOnlyList<TelemetrySnapshotDto> GetAllSnapshots() =>
        _state.Select(kvp => BuildSnapshot(kvp.Key, kvp.Value)).ToList();

    /// <summary>
    /// Raw jagged-array batch snapshot for a sensor — mainly exposed so the
    /// dashboard/demo can show that batches genuinely flow through
    /// <see cref="Telemetry.TelemetryHistoryBuffer{T}"/> rather than being
    /// flattened away before they reach storage.
    /// </summary>
    public double[][]? GetRawBatches(string sensorId) =>
        _state.TryGetValue(sensorId, out var state) ? state.Buffer.SnapshotBatches() : null;

    private TelemetrySnapshotDto BuildSnapshot(string sensorId, SensorTelemetryState state)
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

        return new TelemetrySnapshotDto
        {
            SensorId = sensorId,
            DisplayName = profile is not null ? $"{profile.Location.NodeId} · {profile.Category}" : sensorId,
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
