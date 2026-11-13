namespace SmartX.Shared.DataStructures;

public sealed class ActiveAlertSet
{
    private readonly HashSet<string> _activeKeys = new();
    private readonly object _gate = new();

    public int Count { get { lock (_gate) return _activeKeys.Count; } }

    public static string KeyFor(string sensorId, string alertType) => $"{sensorId}:{alertType}";

    public bool Raise(string key) { lock (_gate) return _activeKeys.Add(key); }

    public bool Clear(string key) { lock (_gate) return _activeKeys.Remove(key); }

    public bool IsActive(string key) { lock (_gate) return _activeKeys.Contains(key); }

    public IReadOnlyCollection<string> Snapshot() { lock (_gate) return _activeKeys.ToList(); }
}
