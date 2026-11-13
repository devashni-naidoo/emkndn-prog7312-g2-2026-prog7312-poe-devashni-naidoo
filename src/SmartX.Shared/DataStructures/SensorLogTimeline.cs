using SmartX.Shared.Commands;

namespace SmartX.Shared.DataStructures;

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

    public IReadOnlyList<LogEntry> ForSensor(string sensorId, int take = 50) =>
        _byTimestamp
            .SelectMany(kvp => kvp.Value)
            .Where(e => e.SensorId == sensorId)
            .TakeLast(take)
            .ToList();


    public IReadOnlyList<LogEntry> InRange(DateTimeOffset fromUtc, DateTimeOffset toUtc) =>
        _byTimestamp

            .Where(kvp => kvp.Key >= fromUtc && kvp.Key <= toUtc)
            .SelectMany(kvp => kvp.Value)
            .ToList();
}
