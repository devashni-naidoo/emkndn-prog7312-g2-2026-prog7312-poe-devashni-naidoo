using SmartX.Shared.Models;

namespace SmartX.Shared.DataStructures;

/// Indexes registered devices by MAC address / unique node identifier for
/// instantaneous O(1) lookups when a new telemetry packet or command
/// arrives, rather than scanning the full device list. 
public sealed class DeviceIndex
{
    private readonly Dictionary<string, DeviceProfile> _byMac = new(StringComparer.OrdinalIgnoreCase);

    public int Count => _byMac.Count;

    public void Put(DeviceProfile profile) => _byMac[profile.MacAddress] = profile;

    public bool Remove(string macAddress) => _byMac.Remove(macAddress);

    public DeviceProfile? FindByMac(string macAddress) =>
        _byMac.TryGetValue(macAddress, out var profile) ? profile : null;

    public bool Contains(string macAddress) => _byMac.ContainsKey(macAddress);

    public IReadOnlyCollection<DeviceProfile> All() => _byMac.Values.ToList();
}
