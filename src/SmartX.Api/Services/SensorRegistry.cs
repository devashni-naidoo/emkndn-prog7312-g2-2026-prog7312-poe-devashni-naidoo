using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using SmartX.Shared.DataStructures;
using SmartX.Shared.Models;
using SmartX.Shared.Validation;

namespace SmartX.Api.Services;

public sealed class SensorRegistry
{
    private static readonly Regex MacAddressPattern =
        new(@"^([0-9A-Fa-f]{2}:){5}[0-9A-Fa-f]{2}$", RegexOptions.Compiled);

    private readonly ConcurrentDictionary<string, DeviceProfile> _sensors = new();

    private readonly DeviceIndex _macIndex = new();
    private readonly object _indexLock = new();

    public IReadOnlyCollection<DeviceProfile> GetAll() => _sensors.Values.ToList();

    public DeviceProfile? Get(string id) => _sensors.TryGetValue(id, out var profile) ? profile : null;

    public DeviceProfile? FindByMac(string macAddress)
    {
        lock (_indexLock)
        {
            return _macIndex.FindByMac(macAddress);
        }
    }

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
