namespace SmartX.Shared.Validation;

/// <summary>
/// The four tiers of the device deployment hierarchy, in required nesting
/// order: a Node must sit inside a Sub-Zone, which must sit inside a Zone,
/// which must sit inside a Facility (e.g. "Sub-Zone B -> Zone 1 -> Facility A").
/// </summary>
public enum DeploymentLevel
{
    Facility = 0,
    Zone = 1,
    SubZone = 2,
    Node = 3
}
