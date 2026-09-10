using SmartX.Shared.Telemetry;

namespace SmartX.Tests;

internal static class TelemetryPacketTests
{
    public static void Run()
    {
        Console.WriteLine("TelemetryPacket<T> (generics)");

        var floatPacket = new TelemetryPacket<float>("sensor-1", 22.5f, "°C");
        Check.AreEqual(22.5f, floatPacket.Value, "float packet stores exact value");

        var intPacket = new TelemetryPacket<int>("sensor-2", 120, "W");
        Check.AreEqual(120, intPacket.Value, "int packet stores exact value");

        var boolPacket = new TelemetryPacket<bool>("sensor-3", true, "");
        Check.AreEqual(true, boolPacket.Value, "bool packet stores exact value");

        Check.AreEqual(22.5, floatPacket.AsDouble(), "AsDouble() converts float correctly");
        Check.AreEqual(1d, boolPacket.AsDouble(), "AsDouble() converts true -> 1");

        Check.That(
            typeof(TelemetryPacket<float>) != typeof(TelemetryPacket<int>),
            "distinct closed generic types are produced per value type T (no shared boxed representation)");
    }
}
