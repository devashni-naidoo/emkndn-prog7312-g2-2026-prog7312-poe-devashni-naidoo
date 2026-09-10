using SmartX.Shared.Telemetry;

namespace SmartX.Tests;

internal static class ZoneLoadMatrixTests
{
    public static void Run()
    {
        Console.WriteLine("ZoneLoadMatrix (multi-dimensional array)");

        var matrix = new ZoneLoadMatrix(new[] { "Zone 1", "Zone 2" });

        var nineAm = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
        var tenAm = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

        matrix.AddReading("Zone 1", nineAm, 100);
        matrix.AddReading("Zone 1", tenAm, 50);
        matrix.AddReading("Zone 2", nineAm, 20);

        Check.AreEqual(150d, matrix.TotalForZone("Zone 1"), "Zone 1 total sums across its hourly buckets");
        Check.AreEqual(20d, matrix.TotalForZone("Zone 2"), "Zone 2 total reflects only its own readings");

        // A zone seen for the first time only at ingestion time should still
        // be accommodated by growing the backing double[,] safely.
        matrix.AddReading("Zone 3", nineAm, 75);
        Check.AreEqual(75d, matrix.TotalForZone("Zone 3"), "a newly seen zone grows the matrix and records correctly");
        Check.AreEqual(3, matrix.Zones.Count, "matrix now tracks three zones");

        var snapshot = matrix.Snapshot();
        Check.AreEqual(3, snapshot.GetLength(0), "snapshot has one row per zone");
        Check.AreEqual(24, snapshot.GetLength(1), "snapshot has 24 hourly columns");
    }
}
