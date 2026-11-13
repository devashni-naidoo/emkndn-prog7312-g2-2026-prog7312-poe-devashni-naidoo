namespace SmartX.Shared.Telemetry;

public readonly struct PowerMeterReading : IEquatable<PowerMeterReading>
{
    public string MeterId { get; }
    public double Watts { get; }
    public DateTimeOffset TimestampUtc { get; }

    public PowerMeterReading(string meterId, double watts, DateTimeOffset? timestampUtc = null)
    {
        MeterId = meterId;
        Watts = watts;
        TimestampUtc = timestampUtc ?? DateTimeOffset.UtcNow;
    }

    //Aggregates the load of two meters: Meter3 = Meter1 + Meter2.
    public static PowerMeterReading operator +(PowerMeterReading a, PowerMeterReading b) =>
        new($"{a.MeterId}+{b.MeterId}", a.Watts + b.Watts);


    public static PowerMeterReading operator -(PowerMeterReading a, PowerMeterReading b) =>
        new($"{a.MeterId}-{b.MeterId}", a.Watts - b.Watts);

    public static bool operator >(PowerMeterReading a, PowerMeterReading b) => a.Watts > b.Watts;
    public static bool operator <(PowerMeterReading a, PowerMeterReading b) => a.Watts < b.Watts;
    public static bool operator >=(PowerMeterReading a, PowerMeterReading b) => a.Watts >= b.Watts;
    public static bool operator <=(PowerMeterReading a, PowerMeterReading b) => a.Watts <= b.Watts;

    public static bool operator ==(PowerMeterReading a, PowerMeterReading b) => a.Equals(b);
    public static bool operator !=(PowerMeterReading a, PowerMeterReading b) => !a.Equals(b);

    public bool Equals(PowerMeterReading other) =>
        MeterId == other.MeterId && Watts.Equals(other.Watts);

    public override bool Equals(object? obj) => obj is PowerMeterReading other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(MeterId, Watts);

    public override string ToString() => $"{MeterId}: {Watts:F1} W @ {TimestampUtc:O}";
}
