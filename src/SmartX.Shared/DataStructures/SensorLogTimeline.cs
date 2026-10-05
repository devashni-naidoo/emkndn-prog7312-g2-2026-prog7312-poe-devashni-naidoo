using SmartX.Shared.Commands;

namespace SmartX.Shared.DataStructures;

/// <summary>
/// Historical sensor/command log entries kept in timestamp order by
/// construction, backed by <see cref="SortedDictionary{TKey,TValue}"/>
/// keyed on the UTC timestamp. Because the dictionary itself maintains
/// order, the dashboard can render a timeline straight off an in-order
/// traversal with no sort step at read time — useful when it's re-queried
/// on every poll. Entries that land on the exact same tick (rare, but
/// possible under load) share a bucket rather than overwriting each other.
/// </summary>
public sealed class SensorLogTimeline
{
    private readonly SortedDictionary<DateTimeOffset, List<LogEntry>> _byTimestamp = new();

    public int Count => _byTimestamp.Values.Sum(bucket => bucket.Count);

    public void Append(LogEntry entry)
    {
        if (!_byTimestamp.TryGetValue(entry.TimestampUtc, out var bucket))
        {
            bucket = new List<LogEntry>();
            _byTimestamp[entry.TimestampUtc] = bucket;
        }
        bucket.Add(entry);
    }

    /// <summary>All entries for one sensor, oldest first, newest last.</summary>
    public IReadOnlyList<LogEntry> ForSensor(string sensorId, int take = 50) =>
        _byTimestamp
            .SelectMany(kvp => kvp.Value)
            .Where(e => e.SensorId == sensorId)
            .TakeLast(take)
            .ToList();

    /// <summary>Every entry across every sensor within an inclusive UTC range, in order.</summary>
    public IReadOnlyList<LogEntry> InRange(DateTimeOffset fromUtc, DateTimeOffset toUtc) =>
        _byTimestamp
            // SortedDictionary keeps this a contiguous, already-ordered slice —
            // no separate sort needed to satisfy the timeline-ordering requirement.
            .Where(kvp => kvp.Key >= fromUtc && kvp.Key <= toUtc)
            .SelectMany(kvp => kvp.Value)
            .ToList();
}
