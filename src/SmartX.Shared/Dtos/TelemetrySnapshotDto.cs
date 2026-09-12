using SmartX.Shared.Models;

namespace SmartX.Shared.Dtos;

/// <summary>
/// What the client dashboard actually renders per sensor card: latest value,
/// percentage delta, status (drives the card colour), a short sparkline
/// history, and freshness. This is the API -> client contract for the
/// real-time visual telemetry feedback engagement strategy.
/// </summary>
public sealed class TelemetrySnapshotDto
{
    public string SensorId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public SensorCategory Category { get; set; }
    public double LatestValue { get; set; }
    public double DeltaPercent { get; set; }
    public TelemetryStatus Status { get; set; }
    public double[] Sparkline { get; set; } = Array.Empty<double>();
    public DateTimeOffset LastUpdatedUtc { get; set; }
    public double SecondsSinceUpdate { get; set; }
}

