using System.Collections.Concurrent;
using SmartX.Shared.Telemetry;

namespace SmartX.Shared.DataStructures;

public readonly record struct TelemetryKey(long Ticks, long Sequence) : IComparable<TelemetryKey>
{
    public int CompareTo(TelemetryKey other)
    {
        int byTicks = Ticks.CompareTo(other.Ticks);
        return byTicks != 0 ? byTicks : Sequence.CompareTo(other.Sequence);
    }
}

public sealed record HistoryQueryResult(
    IReadOnlyList<TelemetryRecord> Records,
    int TreeSize,
    int TreeHeight,
    int NodesVisited);

public sealed class TelemetryHistoryIndex
{
    private sealed class SensorHistory
    {
        public readonly AvlTree<TelemetryKey, TelemetryRecord> Tree = new();
        public readonly object Lock = new();
        public long Sequence;
    }

    private readonly ConcurrentDictionary<string, SensorHistory> _bySensor = new();

    public void Append(string sensorId, DateTimeOffset timestampUtc, double value)
    {
        var history = _bySensor.GetOrAdd(sensorId, _ => new SensorHistory());
        lock (history.Lock)
        {
            var key = new TelemetryKey(timestampUtc.UtcTicks, history.Sequence++);
            history.Tree.Insert(key, new TelemetryRecord(sensorId, timestampUtc, value));
        }
    }

    public int Count(string sensorId) =>
        _bySensor.TryGetValue(sensorId, out var h) ? Locked(h, () => h.Tree.Count) : 0;

    public HistoryQueryResult Query(string sensorId, DateTimeOffset fromUtc, DateTimeOffset toUtc)
    {
        if (!_bySensor.TryGetValue(sensorId, out var history))
        {
            return new HistoryQueryResult(Array.Empty<TelemetryRecord>(), 0, 0, 0);
        }

        lock (history.Lock)
        {
            var low = new TelemetryKey(fromUtc.UtcTicks, long.MinValue);
            var high = new TelemetryKey(toUtc.UtcTicks, long.MaxValue);
            var records = history.Tree.Range(low, high, out int visited);
            return new HistoryQueryResult(records, history.Tree.Count, history.Tree.Height, visited);
        }
    }

    
    public (DateTimeOffset First, DateTimeOffset Last)? Span(string sensorId)
    {
        if (!_bySensor.TryGetValue(sensorId, out var history)) return null;
        lock (history.Lock)
        {
            if (!history.Tree.TryGetMin(out _, out var first) || !history.Tree.TryGetMax(out _, out var last))
                return null;
            return (first!.TimestampUtc, last!.TimestampUtc);
        }
    }

    private static T Locked<T>(SensorHistory h, Func<T> read)
    {
        lock (h.Lock) { return read(); }
    }
}
