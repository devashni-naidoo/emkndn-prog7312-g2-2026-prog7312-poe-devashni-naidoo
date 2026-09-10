using SmartX.Shared.Telemetry;

namespace SmartX.Tests;

internal static class TelemetryHistoryBufferTests
{
    public static void Run()
    {
        Console.WriteLine("TelemetryHistoryBuffer<T> (jagged arrays -> List<T>)");

        var buffer = new TelemetryHistoryBuffer<double>("sensor-1", maxBatches: 3);

        buffer.AddBatch(new double[] { 1.0, 2.0 });      // batch of 2
        buffer.AddBatch(new double[] { 3.0, 4.0, 5.0 }); // batch of 3 -- deliberately different length
        buffer.AddBatch(new double[] { 6.0 });           // batch of 1

        Check.AreEqual(3, buffer.BatchCount, "buffer holds 3 batches after 3 AddBatch calls");

        var raw = buffer.SnapshotBatches();
        Check.AreEqual(2, raw[0].Length, "batch 0 keeps its own length (jagged, not rectangular)");
        Check.AreEqual(3, raw[1].Length, "batch 1 has a different length than batch 0");
        Check.AreEqual(1, raw[2].Length, "batch 2 has yet another length");

        var flattened = buffer.FlushToList("W");
        Check.AreEqual(6, flattened.Count, "FlushToList flattens all batches into one List<TelemetryPacket<T>>");
        Check.AreEqual(1.0, flattened[0].Value, "flattened list preserves original order (first value)");
        Check.AreEqual(6.0, flattened[^1].Value, "flattened list preserves original order (last value)");

        // Ring-buffer eviction: adding a 4th batch should drop the oldest.
        buffer.AddBatch(new double[] { 7.0, 8.0 });
        Check.AreEqual(3, buffer.BatchCount, "buffer stays capped at maxBatches (oldest evicted)");
        var rawAfterEvict = buffer.SnapshotBatches();
        Check.AreEqual(3.0, rawAfterEvict[0][0], "oldest batch (the one starting at 1.0) was evicted");
    }
}
