namespace SmartX.Shared.DataStructures;


public sealed class ActiveAlertSet
{
    private readonly HashSet<string> _activeKeys = new();

    public int Count => _activeKeys.Count;

    public static string KeyFor(string sensorId, string alertType) => $"{sensorId}:{alertType}";

    /// Raises an alert. Returns <c>true</c> only the first time this exact
    /// key becomes active — a caller uses that to decide whether this is a
    /// genuinely new alert worth broadcasting, or one still in effect from
    /// before.

    public bool Raise(string key) => _activeKeys.Add(key);

    public bool Clear(string key) => _activeKeys.Remove(key);

    public bool IsActive(string key) => _activeKeys.Contains(key);

    public IReadOnlyCollection<string> Snapshot() => _activeKeys.ToList();
}
