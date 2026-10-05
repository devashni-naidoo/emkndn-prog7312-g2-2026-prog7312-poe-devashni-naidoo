using SmartX.Shared.Commands;
using SmartX.Shared.DataStructures;
using SmartX.Shared.Models;

namespace SmartX.Tests;

internal static class Part2DataStructureTests
{
    public static void Run()
    {
        CommandQueueTests();
        CriticalAlertQueueTests();
        CommandHistoryStackTests();
        DeviceIndexTests();
        SensorLogTimelineTests();
        ActiveAlertSetTests();
        PatternFrequencyTrackerTests();
    }

    private static DeviceCommand Cmd(string sensorId, CommandType type, CommandSeverity severity = CommandSeverity.Standard) =>
        new() { SensorId = sensorId, Type = type, Severity = severity };

    private static void CommandQueueTests()
    {
        Console.WriteLine("CommandQueue (FIFO Queue<T>)");
        var q = new CommandQueue();
        q.Enqueue(Cmd("s1", CommandType.ToggleRelay));
        q.Enqueue(Cmd("s2", CommandType.OpenValve));
        Check.AreEqual(2, q.Count, "queue holds 2 after 2 enqueues");

        q.TryDequeue(out var first);
        Check.AreEqual("s1", first?.SensorId, "FIFO: first enqueued is first dequeued");
        Check.AreEqual(1, q.Count, "count drops after dequeue");
    }

    private static void CriticalAlertQueueTests()
    {
        Console.WriteLine("CriticalAlertQueue (PriorityQueue<T,long>)");
        var q = new CriticalAlertQueue();

        var older = Cmd("s1", CommandType.ShutValve, CommandSeverity.Critical);
        var newer = new DeviceCommand
        {
            SensorId = "s2", Type = CommandType.TriggerPump, Severity = CommandSeverity.Critical,
            IssuedUtc = older.IssuedUtc.AddSeconds(5)
        };

        // Enqueue the newer one first to prove ordering is by issue time, not insertion order.
        q.Enqueue(newer);
        q.Enqueue(older);

        q.TryDequeue(out var first);
        Check.AreEqual("s1", first?.SensorId, "earlier-issued critical command dequeues first despite being enqueued second");
    }

    private static void CommandHistoryStackTests()
    {
        Console.WriteLine("CommandHistoryStack (Stack<T> + Undo)");
        var stack = new CommandHistoryStack();

        stack.Push(Cmd("s1", CommandType.ShutValve));
        stack.Push(Cmd("s1", CommandType.ToggleRelay));
        Check.AreEqual(2, stack.Count, "stack holds 2 pushed commands");

        var inverse = stack.Undo();
        Check.That(inverse is not null, "undo of ToggleRelay returns a non-null inverse");
        Check.AreEqual(CommandType.ToggleRelay, inverse!.Type, "ToggleRelay's inverse is itself (toggling back)");
        Check.That(inverse.IsUndo, "the generated inverse command is flagged IsUndo");
        Check.AreEqual(1, stack.Count, "stack shrinks by one after Undo pops");

        var inverse2 = stack.Undo();
        Check.AreEqual(CommandType.OpenValve, inverse2!.Type, "ShutValve's inverse is OpenValve");

        var inverse3 = stack.Undo();
        Check.That(inverse3 is null, "undo on an empty stack returns null rather than throwing");
    }

    private static void DeviceIndexTests()
    {
        Console.WriteLine("DeviceIndex (Dictionary<string,DeviceProfile> O(1) lookup)");
        var index = new DeviceIndex();
        var profile = new DeviceProfile { MacAddress = "AA:BB:CC:DD:EE:01", Category = SensorCategory.Actuator };
        index.Put(profile);

        Check.That(index.Contains("AA:BB:CC:DD:EE:01"), "device is found by exact-case MAC");
        Check.That(index.Contains("aa:bb:cc:dd:ee:01"), "lookup is case-insensitive");
        Check.AreEqual(profile.Id, index.FindByMac("AA:BB:CC:DD:EE:01")?.Id, "FindByMac returns the exact registered profile");
        Check.That(index.FindByMac("00:00:00:00:00:00") is null, "unknown MAC returns null, not an exception");

        index.Remove("AA:BB:CC:DD:EE:01");
        Check.That(!index.Contains("AA:BB:CC:DD:EE:01"), "removed device is no longer found");
    }

    private static void SensorLogTimelineTests()
    {
        Console.WriteLine("SensorLogTimeline (SortedDictionary<DateTimeOffset,List<LogEntry>>)");
        var timeline = new SensorLogTimeline();
        var t0 = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

        // Insert deliberately out of chronological order.
        timeline.Append(new LogEntry { SensorId = "s1", TimestampUtc = t0.AddMinutes(5), Message = "third" });
        timeline.Append(new LogEntry { SensorId = "s1", TimestampUtc = t0, Message = "first" });
        timeline.Append(new LogEntry { SensorId = "s1", TimestampUtc = t0.AddMinutes(2), Message = "second" });

        var ordered = timeline.ForSensor("s1");
        Check.AreEqual(3, ordered.Count, "all 3 entries retrieved for the sensor");
        Check.AreEqual("first", ordered[0].Message, "entries come back in timestamp order despite insertion order (1/3)");
        Check.AreEqual("second", ordered[1].Message, "entries come back in timestamp order despite insertion order (2/3)");
        Check.AreEqual("third", ordered[2].Message, "entries come back in timestamp order despite insertion order (3/3)");

        var inRange = timeline.InRange(t0, t0.AddMinutes(3));
        Check.AreEqual(2, inRange.Count, "InRange returns only entries within the inclusive window");
    }

    private static void ActiveAlertSetTests()
    {
        Console.WriteLine("ActiveAlertSet (HashSet<string> deduplication)");
        var set = new ActiveAlertSet();
        string key = ActiveAlertSet.KeyFor("s1", "Disconnected");

        Check.That(set.Raise(key), "first Raise() of a new alert returns true (newly active)");
        Check.That(!set.Raise(key), "re-raising the same still-active alert returns false (deduplicated)");
        Check.AreEqual(1, set.Count, "duplicate raises never grow the set past 1 entry for the same key");

        Check.That(set.IsActive(key), "alert reads as active");
        Check.That(set.Clear(key), "clearing an active alert returns true");
        Check.That(!set.IsActive(key), "alert reads as inactive after clearing");
        Check.That(!set.Clear(key), "clearing an already-cleared alert returns false, not an exception");
    }

    private static void PatternFrequencyTrackerTests()
    {
        Console.WriteLine("PatternFrequencyTracker (predictive insights engine)");
        var tracker = new PatternFrequencyTracker();

        tracker.RecordCommandDuringAlert("Anomalous", CommandType.StopPump);
        tracker.RecordCommandDuringAlert("Anomalous", CommandType.TriggerPump);
        tracker.RecordCommandDuringAlert("Anomalous", CommandType.TriggerPump);

        var best = tracker.MostLikelyResponseTo("Anomalous");
        Check.That(best is not null, "a suggestion is produced once the occurrence floor is met");
        Check.AreEqual(CommandType.TriggerPump, best!.Value.Command, "the more frequent command (2x) wins over the 1x alternative");
        Check.AreEqual(2, best.Value.Occurrences, "occurrence count is reported accurately");

        var none = tracker.MostLikelyResponseTo("NeverSeenAlertType");
        Check.That(none is null, "an alert type with no history produces no suggestion");

        tracker.RecordSearch("Pump A");
        tracker.RecordSearch("Pump A");
        tracker.RecordSearch("Moisture Sensor B");
        var top = tracker.TopSearchTerms(1);
        Check.AreEqual("Pump A", top[0].Term, "most frequently searched term is ranked first");
    }
}
