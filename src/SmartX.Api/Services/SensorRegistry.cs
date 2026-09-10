using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<string, DeviceProfile> _sensors = new();

    public IReadOnlyCollection<DeviceProfile> GetAll() => _sensors.Values.ToList();

    public DeviceProfile? Get(string id) => _sensors.TryGetValue(id, out var profile) ? profile : null;

    /// <summary>
    /// Validates the deployment hierarchy recursively (via
    /// <see cref="DeploymentValidator"/>) before accepting the registration.
    /// </summary>
    public (bool Success, string Message, DeviceProfile? Profile) Register(
        string macAddress, string facility, string zone, string subZone, string room,
        string nodeId, SensorCategory category)
    {
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
        if (_sensors.Values.Any(existing =>
                string.Equals(existing.MacAddress, macAddress, StringComparison.OrdinalIgnoreCase)))
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
        return (true, validation.Message, profile);
    }

    public bool AddAttachment(string sensorId, MediaAttachment attachment)
    {
        if (!_sensors.TryGetValue(sensorId, out var profile)) return false;
        profile.Attachments.Add(attachment);
        return true;
    }
}
