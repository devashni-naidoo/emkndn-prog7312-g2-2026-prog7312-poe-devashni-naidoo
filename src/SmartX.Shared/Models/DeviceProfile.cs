namespace SmartX.Shared.Models;

public sealed class DeviceProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string MacAddress { get; set; } = string.Empty;
    public DeploymentLocation Location { get; set; } = new();
    public SensorCategory Category { get; set; }
    public List<MediaAttachment> Attachments { get; set; } = new();
    public DateTimeOffset RegisteredUtc { get; set; } = DateTimeOffset.UtcNow;

    public double AnomalyThresholdPercent => Category switch
    {
        SensorCategory.PowerConsumption => 40.0,
        SensorCategory.Environmental => 25.0,
        SensorCategory.Actuator => 100.0,
        _ => 30.0
    };
}
