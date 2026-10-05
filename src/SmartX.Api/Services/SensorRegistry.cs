using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using SmartX.Shared.DataStructures;
using SmartX.Shared.Models;
using SmartX.Shared.Validation;

namespace SmartX.Api.Services;

/// <summary>
/// Thread-safe in-memory store of registered <see cref="DeviceProfile"/>
/// records. A real deployment would back this with a database, but the
/// module brief scopes Part 1 to the ingestion/validation gateway itself, so
/// an in-process registry keeps the sample self-contained and dependency-free.
/// </summary>
public sealed class SensorRegistry
{
    private static readonly Regex MacAddressPattern =
        new(@"^([0-9A-Fa-f]{2}:){5}[0-9A-Fa-f]{2}$", RegexOptions.Compiled);

    private readonly ConcurrentDictionary<string, DeviceProfile> _sensors = new();

    // O(1) MAC -> device lookup (Part 2 technical requirement), kept in sync
    // with _sensors by Register()/AddAttachment() below. Access is guarded
    // by a lock since DeviceIndex's backing Dictionary<TKey,TValue> is not
    // itself thread-safe, unlike the ConcurrentDictionary used for the
    // primary store.
    private readonly DeviceIndex _macIndex = new();
    private readonly object _indexLock = new();

    public IReadOnlyCollection<DeviceProfile> GetAll() => _sensors.Values.ToList();

    public DeviceProfile? Get(string id) => _sensors.TryGetValue(id, out var profile) ? profile : null;

    /// <summary>O(1) lookup by MAC address, used when a telemetry packet or command arrives.</summary>
    public DeviceProfile? FindByMac(string macAddress)
    {
        lock (_indexLock)
        {
            return _macIndex.FindByMac(macAddress);
        }
    }

    /// <summary>
    /// Validates the deployment hierarchy recursively (via
    /// <see cref="DeploymentValidator"/>) before accepting the registration.
    /// </summary>
    public (bool Success, string Message, DeviceProfile? Profile) Register(
        string macAddress, string facility, string zone, string subZone, string room,
        string nodeId, SensorCategory category)
    {
        facility = facility?.Trim() ?? string.Empty;
        zone = zone?.Trim() ?? string.Empty;
        subZone = subZone?.Trim() ?? string.Empty;
        room = room?.Trim() ?? string.Empty;
        nodeId = nodeId?.Trim() ?? string.Empty;

        var validation = DeploymentValidator.ValidateFlat(facility, zone, subZone, nodeId);
        if (!validation.Success)
        {
            return (false, validation.Message, null);
        }

        if (string.IsNullOrWhiteSpace(macAddress))
        {
            return (false, "MAC address / unique identifier is required.", null);
        }

        macAddress = macAddress.Trim().ToUpperInvariant();
        if (!MacAddressPattern.IsMatch(macAddress))
        {
            return (false, "MAC address must be in the form AA:BB:CC:DD:EE:FF.", null);
        }

        lock (_indexLock)
        {
            if (_macIndex.Contains(macAddress))
            {
                return (false, $"A sensor with MAC address '{macAddress}' is already registered.", null);
            }

            var profile = new DeviceProfile
            {
                MacAddress = macAddress,
                Category = category,
                Location = new DeploymentLocation
                {
                    Facility = facility,
                    Zone = zone,
                    SubZone = subZone,
                    Room = room,
                    NodeId = nodeId
                }
            };

            _sensors[profile.Id] = profile;
            _macIndex.Put(profile);
            return (true, validation.Message, profile);
        }
    }

    public bool AddAttachment(string sensorId, MediaAttachment attachment)
    {
        if (!_sensors.TryGetValue(sensorId, out var profile)) return false;
        profile.Attachments.Add(attachment);
        return true;
    }
}
