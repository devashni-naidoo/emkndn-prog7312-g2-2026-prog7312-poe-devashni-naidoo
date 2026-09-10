using SmartX.Shared.Telemetry;

namespace SmartX.Tests;

internal static class PowerMeterReadingTests
{
    public static void Run()
    {
        Console.WriteLine("PowerMeterReading (operator overloading)");

        var meter1 = new PowerMeterReading("Meter1", 300);
        var meter2 = new PowerMeterReading("Meter2", 80);

        var meter3 = meter1 + meter2;
        Check.AreEqual(380d, meter3.Watts, "Meter3 = Meter1 + Meter2 aggregates watts");

        var delta = meter1 - meter2;
        Check.AreEqual(220d, delta.Watts, "Meter1 - Meter2 computes delta watts");

        Check.That(meter1 > meter2, "operator > compares meters by watts");
        Check.That(!(meter2 > meter1), "operator > is directionally correct");
        Check.That(meter1 != meter2, "operator != distinguishes different readings");

        var meter1Copy = new PowerMeterReading("Meter1", 300);
        Check.That(meter1 == meter1Copy, "operator == treats equal id+watts as equal");
    }
}
