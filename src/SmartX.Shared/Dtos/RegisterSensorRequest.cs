using SmartX.Shared.Models;

namespace SmartX.Shared.Dtos;

/// <summary>
/// Flat registration payload posted by the dashboard's registration form.
/// The API turns Facility/Zone/SubZone/NodeId into a nested
/// <see cref="Validation.DeploymentNode"/> tree and recursively validates it
/// before a <see cref="DeviceProfile"/> is created.
/// </summary>
public sealed class RegisterSensorRequest
{
    public string MacAddress { get; set; } = string.Empty;
    public string Facility { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public string SubZone { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public SensorCategory Category { get; set; }
}
