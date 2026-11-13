using SmartX.Shared.Commands;

namespace SmartX.Shared.DataStructures;

public sealed class SensorLogTimeline
{
    private readonly SortedDictionary<DateTimeOffset, List<LogEntry>> _byTimestamp = new();
    private readonly object _gate = new();

    public int Count
    {
        get
        {
            lock (_gate) return _byTimestamp.Values.Sum(bucket => bucket.Count);
        }
    }

    public void Append(LogEntry entry)
    {
        lock (_gate)
        {
            if (!_byTimestamp.TryGetValue(entry.TimestampUtc, out var bucket))
            {
                bucket = new List<LogEntry>();
                _byTimestamp[entry.TimestampUtc] = bucket;
            }
            bucket.Add(entry);
        }
    }

    public IReadOnlyList<LogEntry> ForSensor(string sensorId, int take = 50)
    {
        lock (_gate)
        {
            return _byTimestamp
                .SelectMany(kvp => kvp.Value)
                .Where(e => e.SensorId == sensorId)
                .TakeLast(take)
                .ToList();
        }
    }

    public IReadOnlyList<LogEntry> InRange(DateTimeOffset fromUtc, DateTimeOffset toUtc)
    {
        lock (_gate)
        {
            return _byTimestamp
                // SortedDictionary keeps this a contiguous, already-ordered slice -
                // no separate sort needed to satisfy the timeline-ordering requirement.
                .Where(kvp => kvp.Key >= fromUtc && kvp.Key <= toUtc)
                .SelectMany(kvp => kvp.Value)
                .ToList();
        }
    }
}
