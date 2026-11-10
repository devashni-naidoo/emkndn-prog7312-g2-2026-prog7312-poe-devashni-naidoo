using SmartX.Shared.DataStructures;

namespace SmartX.Tests;

internal static class Part3DataStructureTests
{
    public static void Run()
    {
        AvlTreeTests();
        TelemetryHistoryIndexTests();
        MinHeapTests();
        MeshGraphTests();
        PrimMatchesKruskalTests();
    }

    private static void AvlTreeTests()
    {
        Console.WriteLine("AvlTree (self-balancing BST)");

        // Sorted insertion is the worst case for a plain BST (it degenerates into a linked list).
        var tree = new AvlTree<int, string>();
        for (int i = 1; i <= 1000; i++) tree.Insert(i, $"v{i}");

        Check.AreEqual(1000, tree.Count, "1000 ascending inserts are all stored");
        Check.That(tree.IsValidAvl(), "AVL invariants hold (BST order, heights, balance factors in [-1,1])");
        Check.That(tree.Height <= 14, $"height stays logarithmic (got {tree.Height}, a plain BST would be 1000)");
        Check.That(tree.RotationCount > 0, "rotations were performed to keep the tree balanced");

        tree.Insert(500, "replaced");
        Check.AreEqual(1000, tree.Count, "re-inserting an existing key replaces rather than duplicates");
        tree.TryGet(500, out var replaced);
        Check.AreEqual("replaced", replaced, "TryGet returns the replacement value");
        Check.That(!tree.TryGet(5000, out _), "TryGet reports a missing key");

        tree.TryGetMin(out var min, out _);
        tree.TryGetMax(out var max, out _);
        Check.AreEqual(1, min, "minimum key found");
        Check.AreEqual(1000, max, "maximum key found");

        // Range query: inclusive bounds, ascending order, and only a sliver of the tree is visited.
        var range = tree.Range(400, 410, out int visited);
        Check.AreEqual(11, range.Count, "range [400,410] returns 11 values (inclusive)");
        Check.AreEqual("v400", range[0], "range starts at the lower bound");
        Check.That(visited < 60, $"range query touched only {visited} of 1000 nodes (O(log n + k))");
        Check.AreEqual(0, tree.Range(2000, 3000, out _).Count, "range beyond the data is empty");
        Check.AreEqual(0, tree.Range(10, 5, out _).Count, "inverted range is empty");

        // Randomised insertion order: compare against a brute-force filter.
        var random = new Random(7);
        var keys = Enumerable.Range(0, 3000).OrderBy(_ => random.Next()).ToList();
        var shuffled = new AvlTree<int, int>();
        foreach (var k in keys) shuffled.Insert(k, k);
        Check.That(shuffled.IsValidAvl(), "AVL invariants hold after 3000 shuffled inserts");

        bool allMatch = true;
        for (int trial = 0; trial < 50; trial++)
        {
            int lo = random.Next(0, 3000), hi = lo + random.Next(0, 400);
            var expected = keys.Where(k => k >= lo && k <= hi).OrderBy(k => k).ToList();
            var actual = shuffled.Range(lo, hi, out _);
            if (!expected.SequenceEqual(actual)) allMatch = false;
        }
        Check.That(allMatch, "50 random range queries match a brute-force scan");
        Check.That(shuffled.InOrder().SequenceEqual(Enumerable.Range(0, 3000)), "in-order traversal is sorted");
    }

    private static void TelemetryHistoryIndexTests()
    {
        Console.WriteLine("TelemetryHistoryIndex (per-sensor AVL tree on timestamp)");
        var index = new TelemetryHistoryIndex();
        var day = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

        // One reading a minute for a full day, deliberately inserted out of order.
        var minutes = Enumerable.Range(0, 24 * 60).OrderBy(m => (m * 7919) % 1441).ToList();
        foreach (int m in minutes) index.Append("node-a", day.AddMinutes(m), 20 + m % 10);
        index.Append("node-b", day.AddHours(15), 99);

        var result = index.Query("node-a", day.AddHours(14), day.AddHours(16));
        Check.AreEqual(121, result.Records.Count, "14:00-16:00 inclusive returns 121 one-minute readings");
        Check.That(result.Records.All(r => r.SensorId == "node-a"), "other sensors' readings are never returned");
        Check.That(result.Records.Zip(result.Records.Skip(1), (a, b) => a.TimestampUtc <= b.TimestampUtc).All(x => x),
            "results come back in timestamp order");
        Check.AreEqual(day.AddHours(14), result.Records[0].TimestampUtc, "first reading is exactly 14:00");
        Check.That(result.NodesVisited < 200, $"visited {result.NodesVisited} of {result.TreeSize} nodes, not the whole day");
        Check.That(result.TreeHeight <= 15, $"tree height {result.TreeHeight} is logarithmic for 1440 readings");

        index.Append("node-a", day.AddHours(14), 123);
        Check.AreEqual(122, index.Query("node-a", day.AddHours(14), day.AddHours(16)).Records.Count,
            "two readings on the same tick are both kept");
        Check.AreEqual(0, index.Query("ghost", day, day.AddDays(1)).Records.Count, "unknown sensor yields an empty result");
        Check.AreEqual(1, index.Count("node-b"), "readings are tracked per sensor");

        var span = index.Span("node-a");
        Check.AreEqual(day, span?.First, "span reports the earliest reading");
        Check.AreEqual(day.AddMinutes(1439), span?.Last, "span reports the latest reading");
    }

    private static void MinHeapTests()
    {
        Console.WriteLine("MinHeap (binary heap)");
        var heap = new MinHeap<string, double>();
        foreach (var (name, p) in new[] { ("e", 5.0), ("a", 1.0), ("d", 4.0), ("b", 2.0), ("c", 3.0), ("a2", 1.0) })
            heap.Push(name, p);

        Check.AreEqual(6, heap.Count, "6 items pushed");
        Check.That(heap.Peek() is "a" or "a2", "peek returns a lowest-priority element");

        var order = new List<double>();
        while (heap.TryPop(out _, out var priority)) order.Add(priority);
        Check.That(order.SequenceEqual(new[] { 1.0, 1.0, 2.0, 3.0, 4.0, 5.0 }), "pops come out in ascending priority");
        Check.That(!heap.TryPop(out _, out _), "popping an empty heap returns false");

        var random = new Random(3);
        var big = new MinHeap<int, int>();
        for (int i = 0; i < 2000; i++) big.Push(i, random.Next(10000));
        int previous = int.MinValue;
        bool sorted = true;
        while (big.TryPop(out _, out var p)) { if (p < previous) sorted = false; previous = p; }
        Check.That(sorted, "2000 random pushes pop in non-decreasing order");
    }

    //        S --4-- A --5-- C --3-- D
    //        |      /         /
    //        1    2         8
    //        |  /         /
    //        B ----------
    private static MeshGraph SampleGraph()
    {
        var g = new MeshGraph();
        foreach (var id in new[] { "S", "A", "B", "C", "D" })
            g.AddVertex(new MeshVertex(id, id, id == "S" ? MeshNodeKind.Server : MeshNodeKind.Sensor, "Z"));
        g.AddEdge("S", "A", 4);
        g.AddEdge("S", "B", 1);
        g.AddEdge("A", "B", 2);
        g.AddEdge("A", "C", 5);
        g.AddEdge("B", "C", 8);
        g.AddEdge("C", "D", 3);
        return g;
    }

    private static void MeshGraphTests()
    {
        Console.WriteLine("MeshGraph (BFS / DFS / Prim / Kruskal)");
        var g = SampleGraph();

        Check.AreEqual(5, g.Vertices.Count, "5 vertices");
        Check.AreEqual(6, g.Edges.Count, "6 links");
        Check.That(!g.AddEdge("A", "S", 9), "a duplicate link (either direction) is rejected");
        Check.That(!g.AddEdge("A", "A", 1), "self-loops are rejected");

        var bfs = g.Bfs("S");
        Check.AreEqual("S", bfs.VisitOrder[0], "BFS starts at the source");
        Check.That(bfs.VisitOrder.Take(3).ToHashSet().SetEquals(new[] { "S", "A", "B" }), "BFS visits all 1-hop neighbours before anything deeper");
        Check.AreEqual(5, bfs.VisitOrder.Count, "BFS reaches every vertex of a connected graph");

        var dfs = g.Dfs("S");
        Check.AreEqual(5, dfs.VisitOrder.Count, "DFS reaches every vertex of a connected graph");
        Check.That(dfs.VisitOrder.ToHashSet().SetEquals(bfs.VisitOrder), "DFS and BFS find the same set of devices");
        Check.AreEqual("A", dfs.VisitOrder[1], "DFS dives down the first branch before backtracking");

        // Node B fails: everything is still reachable, but via A.
        bool NotB(string id) => id != "B";
        var withoutB = g.Bfs("S", NotB);
        Check.That(!withoutB.VisitOrder.Contains("B"), "a down node is never visited");
        Check.That(withoutB.VisitOrder.Contains("C") && withoutB.VisitOrder.Contains("D"), "devices behind the failure are still reachable via another hop");

        // Both A and B fail: C and D are cut off.
        bool NotAB(string id) => id is not ("A" or "B");
        Check.That(g.ShortestHopPath("S", "D", NotAB) is null, "no path when every route is down");
        Check.That(g.Bfs("S", id => id != "S").VisitOrder.Count == 0, "traversal from a down source visits nothing");

        var hopPath = g.ShortestHopPath("S", "D");
        Check.AreEqual(4, hopPath?.Count, "fewest-hop path S->D has 3 hops");
        var rerouted = g.ShortestHopPath("S", "D", NotB);
        Check.That(rerouted is not null && rerouted.SequenceEqual(new[] { "S", "A", "C", "D" }), "with B down, S->D reroutes S,A,C,D");

        // Minimum spanning tree.
        var prim = g.Prim("S");
        Check.AreEqual(11.0, prim.TotalCost, "Prim's MST total cost is 11 (1+2+5+3)");
        Check.AreEqual(4, prim.Edges.Count, "MST of 5 vertices has 4 links");
        Check.AreEqual(5, prim.VerticesSpanned, "MST spans all 5 vertices");
        Check.AreEqual(11.0, g.Kruskal().TotalCost, "Kruskal independently agrees on cost 11");

        var treePath = MeshGraph.PathInTree(prim, "D", "S");
        Check.That(treePath is not null && treePath.SequenceEqual(new[] { "D", "C", "A", "B", "S" }), "route along the MST from D to S");
        Check.AreEqual(11.0, g.PathCost(treePath!), "cost of that tree route");

        var primWithoutB = g.Prim("S", NotB);
        Check.AreEqual(12.0, primWithoutB.TotalCost, "MST recomputed with B down: S-A 4 + A-C 5 + C-D 3 = 12");
        Check.AreEqual(4, primWithoutB.VerticesSpanned, "MST spans the 4 surviving vertices");

        var isolated = g.Prim("S", NotAB);
        Check.AreEqual(1, isolated.VerticesSpanned, "with A and B down only the server remains in the tree");
        Check.That(MeshGraph.PathInTree(isolated, "D", "S") is null, "isolated node has no tree route");
    }

    private static void PrimMatchesKruskalTests()
    {
        Console.WriteLine("Prim vs Kruskal on random meshes");
        var random = new Random(11);
        bool allEqual = true;
        bool alwaysSpanning = true;

        for (int trial = 0; trial < 40; trial++)
        {
            var g = new MeshGraph();
            int n = random.Next(5, 30);
            for (int i = 0; i < n; i++) g.AddVertex(new MeshVertex($"n{i}", $"n{i}", MeshNodeKind.Sensor, "Z"));
            for (int i = 1; i < n; i++) g.AddEdge($"n{i}", $"n{random.Next(0, i)}", Math.Round(1 + random.NextDouble() * 9, 1)); // connected
            for (int extra = 0; extra < n * 2; extra++)
                g.AddEdge($"n{random.Next(n)}", $"n{random.Next(n)}", Math.Round(1 + random.NextDouble() * 9, 1));

            var prim = g.Prim("n0");
            var kruskal = g.Kruskal();
            if (Math.Abs(prim.TotalCost - kruskal.TotalCost) > 0.001) allEqual = false;
            if (prim.Edges.Count != n - 1 || prim.VerticesSpanned != n) alwaysSpanning = false;
        }

        Check.That(allEqual, "Prim's and Kruskal's total cost agree on 40 random meshes (so Prim's result is minimal)");
        Check.That(alwaysSpanning, "every MST has exactly n-1 links and spans every node");
    }
}
