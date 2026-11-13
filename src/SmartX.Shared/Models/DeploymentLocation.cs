namespace SmartX.Shared.Models;

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
