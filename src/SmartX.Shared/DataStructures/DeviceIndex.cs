using SmartX.Shared.Models;

namespace SmartX.Shared.DataStructures;

public sealed class DeviceIndex
{
    private readonly Dictionary<string, DeviceProfile> _byMac = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    public int Count { get { lock (_gate) return _byMac.Count; } }

    public void Put(DeviceProfile profile) { lock (_gate) _byMac[profile.MacAddress] = profile; }

    public bool Remove(string macAddress) { lock (_gate) return _byMac.Remove(macAddress); }

    public DeviceProfile? FindByMac(string macAddress)
    {
        lock (_gate) return _byMac.TryGetValue(macAddress, out var profile) ? profile : null;
    }

    public bool Contains(string macAddress) { lock (_gate) return _byMac.ContainsKey(macAddress); }

    public IReadOnlyCollection<DeviceProfile> All() { lock (_gate) return _byMac.Values.ToList(); }
}
