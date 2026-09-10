namespace SmartX.Shared.Dtos;

/// <summary>Single-reading ingestion payload.</summary>
public sealed class IngestReadingRequest
{
    public string SensorId { get; set; } = string.Empty;
    public double Value { get; set; }
}

/// <summary>
/// Bulk-batch ingestion payload — demonstrates the jagged-array batch path
/// (<see cref="Telemetry.TelemetryHistoryBuffer{T}"/>) handled server-side,
/// e.g. when a sensor uploads a burst of buffered readings at once after
/// reconnecting.
/// </summary>
public sealed class IngestBatchRequest
{
    public string SensorId { get; set; } = string.Empty;
    public double[] Values { get; set; } = Array.Empty<double>();
}

/// <summary>
/// Requests the API aggregate two meters' latest readings using the
/// overloaded <c>+</c> operator on <see cref="Telemetry.PowerMeterReading"/>.
/// </summary>
public sealed class AggregateMetersRequest
{
    public string MeterIdA { get; set; } = string.Empty;
    public string MeterIdB { get; set; } = string.Empty;
}
