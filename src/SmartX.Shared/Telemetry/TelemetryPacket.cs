namespace SmartX.Shared.Telemetry;

public sealed class TelemetryPacket<T> where T : struct
{
    public string SensorId { get; }
    public T Value { get; }
    public string Unit { get; }
    public DateTimeOffset TimestampUtc { get; }

    public TelemetryPacket(string sensorId, T value, string unit = "", DateTimeOffset? timestampUtc = null)
    {
        if (string.IsNullOrWhiteSpace(sensorId))
            throw new ArgumentException("SensorId is required.", nameof(sensorId));

        SensorId = sensorId;
        Value = value;
        Unit = unit;
        TimestampUtc = timestampUtc ?? DateTimeOffset.UtcNow;
    }

    public double AsDouble() => Value switch
    {
        double d => d,
        float f => f,
        int i => i,
        long l => l,
        bool b => b ? 1d : 0d,
        _ => Convert.ToDouble(Value)
    };

    public override string ToString() => $"[{SensorId}] {Value}{Unit} @ {TimestampUtc:O}";
}
