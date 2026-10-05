namespace SmartX.Shared.DataStructures;

/// <summary>
/// Tracks currently active, unique error/disconnect states, backed by
/// <see cref="HashSet{T}"/>. Because a set only ever stores an element once,
/// this is what stops the system from re-processing or re-broadcasting a
/// duplicate "sensor disconnected" alert every time the anomaly engine
/// re-evaluates the same still-disconnected sensor — the alert key simply
/// fails to get added a second time.
/// </summary>
public sealed class ActiveAlertSet
{
    private readonly HashSet<string> _activeKeys = new();

    public int Count => _activeKeys.Count;

    public static string KeyFor(string sensorId, string alertType) => $"{sensorId}:{alertType}";

    /// <summary>
    /// Raises an alert. Returns <c>true</c> only the first time this exact
    /// key becomes active — a caller uses that to decide whether this is a
    /// genuinely new alert worth broadcasting, or one still in effect from
    /// before.
    /// </summary>
    public bool Raise(string key) => _activeKeys.Add(key);

    /// <summary>Clears an alert once it's resolved. Returns false if it wasn't active.</summary>
    public bool Clear(string key) => _activeKeys.Remove(key);

    public bool IsActive(string key) => _activeKeys.Contains(key);

    public IReadOnlyCollection<string> Snapshot() => _activeKeys.ToList();
}
