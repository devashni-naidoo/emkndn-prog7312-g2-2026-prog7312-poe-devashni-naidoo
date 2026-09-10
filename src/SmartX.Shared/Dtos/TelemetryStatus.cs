namespace SmartX.Shared.Dtos;

/// <summary>
/// Drives the threshold-based colour coding / anomaly-highlighting dashboard
/// engagement strategy selected in the Task 1 research report.
/// </summary>
public enum TelemetryStatus
{
    Normal,
    Warning,
    Anomalous,
    Disconnected
}
