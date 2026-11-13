namespace SmartX.Shared.Telemetry;

public sealed record TelemetryRecord(string SensorId, DateTimeOffset TimestampUtc, double Value);
