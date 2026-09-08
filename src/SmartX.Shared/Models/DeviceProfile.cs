namespace SmartX.Shared.Models;

/// <summary>
/// A registered (simulated or attached) sensor device: MAC / unique identifier,
/// deployment location, category selection and any attached media/log files.
/// This is the aggregate the "Sensor Payload Management" requirement describes.
/// </summary>
public sealed class DeviceProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string MacAddress { get; set; } = string.Empty;
    public DeploymentLocation Location { get; set; } = new();
    public SensorCategory Category { get; set; }
    public List<MediaAttachment> Attachments { get; set; } = new();
    public DateTimeOffset RegisteredUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Threshold above which a percentage delta between consecutive readings
    /// is treated as anomalous. Category-specific so a power meter and a
    /// binary actuator don't share the same sensitivity.
    /// </summary>
    public double AnomalyThresholdPercent => Category switch
    {
        SensorCategory.PowerConsumption => 40.0,
        SensorCategory.Environmental => 25.0,
        SensorCategory.Actuator => 100.0,
        _ => 30.0
    };
}
