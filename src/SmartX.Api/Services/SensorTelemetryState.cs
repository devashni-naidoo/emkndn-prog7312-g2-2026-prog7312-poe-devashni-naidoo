using SmartX.Shared.Telemetry;

namespace SmartX.Api.Services;

internal sealed class SensorTelemetryState
{
    public TelemetryHistoryBuffer<double> Buffer { get; }
    public List<double> RecentValues { get; } = new();
    public double LastValue { get; set; }
    public double PreviousValue { get; set; }
    public DateTimeOffset LastUpdatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public SensorTelemetryState(string sensorId)
    {
        Buffer = new TelemetryHistoryBuffer<double>(sensorId, maxBatches: 20);
    }
}
