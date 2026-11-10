namespace SmartX.Shared.Telemetry;

/// <summary>One timestamped historical reading for a sensor.</summary>
public sealed record TelemetryRecord(string SensorId, DateTimeOffset TimestampUtc, double Value);
