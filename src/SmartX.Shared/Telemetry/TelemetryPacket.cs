namespace SmartX.Shared.Telemetry;

/// <summary>
/// Reusable generic wrapper that lets the ingestion pipeline handle disparate
/// incoming sensor data types uniformly: a <c>float</c> temperature reading,
/// an <c>int</c> power-metric reading, or a <c>bool</c> smart-switch trigger
/// can all be carried through the same pipeline as a
/// <see cref="TelemetryPacket{T}"/> without ever being boxed to
/// <see cref="object"/>.
/// </summary>
/// <remarks>
/// The <c>where T : struct</c> constraint is the mechanism that avoids boxing:
/// for a constrained generic value type, the CLR JIT-compiles a distinct,
/// specialised native implementation of <see cref="TelemetryPacket{T}"/> per
/// concrete <typeparamref name="T"/> (one for <c>float</c>, one for
/// <c>int</c>, one for <c>bool</c>, etc). <see cref="Value"/> is therefore
/// stored inline as that value type, not as a heap-allocated, boxed
/// <see cref="object"/> the way an unconstrained <c>TelemetryPacket&lt;object&gt;</c>
/// or a non-generic "object payload" design would require. This removes both
/// the allocation and the unbox-on-read cost that a type-coercing design
/// would pay on every single ingested reading.
/// </remarks>
/// <typeparam name="T">
/// The value type carried by this packet, e.g. <c>float</c>, <c>int</c>,
/// <c>double</c> or <c>bool</c>.
/// </typeparam>
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

    /// <summary>
    /// Converts the strongly typed <see cref="Value"/> to <see cref="double"/>
    /// for uniform aggregation/threshold logic further up the pipeline. This
    /// is the one deliberate, explicit conversion point in the system — it
    /// happens once per packet rather than requiring every downstream
    /// consumer to box/unbox or reflect on <typeparamref name="T"/>.
    /// </summary>
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
