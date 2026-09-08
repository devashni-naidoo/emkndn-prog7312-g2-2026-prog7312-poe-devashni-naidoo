namespace SmartX.Shared.Models;

/// <summary>
/// Physical / logical deployment address of a sensor: Room, Zone and Node ID,
/// as required by the sensor payload management functional requirement.
/// This is also the leaf-level detail used to build the
/// Facility -> Zone -> Sub-Zone -> Node hierarchy validated recursively
/// by <see cref="SmartX.Shared.Validation.DeploymentValidator"/>.
/// </summary>
public sealed class DeploymentLocation
{
    public string Facility { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public string SubZone { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;

    public override string ToString() =>
        $"{Facility} -> {Zone} -> {SubZone} -> {NodeId} (Room: {Room})";
}
