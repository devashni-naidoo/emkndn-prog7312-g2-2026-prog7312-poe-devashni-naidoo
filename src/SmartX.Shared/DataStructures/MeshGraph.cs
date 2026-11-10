namespace SmartX.Shared.DataStructures;

public enum MeshNodeKind
{
    Server,
    Gateway,
    Sensor
}

/// <summary>A vertex of the mesh: the central server, a zone gateway, or an ESP32 sensor node.</summary>
public sealed record MeshVertex(string Id, string Label, MeshNodeKind Kind, string Zone);

/// <summary>An undirected wireless link. <see cref="Cost"/> is the relative power cost of hopping across it.</summary>
public sealed record MeshEdge(string A, string B, double Cost)
{
    public string Other(string id) => id == A ? B : A;
}

/// <summary>Order in which a traversal reached each vertex, plus each vertex's parent.</summary>
public sealed record TraversalResult(
    IReadOnlyList<string> VisitOrder,
    IReadOnlyDictionary<string, string?> Parent);

/// <summary>A minimum spanning tree (or forest, if the graph is disconnected).</summary>
public sealed record SpanningTreeResult(IReadOnlyList<MeshEdge> Edges, double TotalCost, int VerticesSpanned);

/// <summary>
/// Undirected weighted graph modelling the physical IoT mesh (Part 3 -
/// Graphs). Vertices are the ESP32 sensors (plus zone gateways and the
/// central server); edges are wireless connections. Stored as an adjacency
/// list. Every algorithm takes an optional <c>passable</c> predicate so a
/// failed (down) node can simply be excluded from the traversal without
/// mutating the graph.
/// </summary>
public sealed class MeshGraph
{
    private readonly Dictionary<string, MeshVertex> _vertices = new();
    private readonly Dictionary<string, List<MeshEdge>> _adjacency = new();
    private readonly List<MeshEdge> _edges = new();

    public IReadOnlyCollection<MeshVertex> Vertices => _vertices.Values;
    public IReadOnlyList<MeshEdge> Edges => _edges;

    public bool Contains(string id) => _vertices.ContainsKey(id);

    public MeshVertex? Get(string id) => _vertices.TryGetValue(id, out var v) ? v : null;

    public void AddVertex(MeshVertex vertex)
    {
        if (_vertices.TryAdd(vertex.Id, vertex))
        {
            _adjacency[vertex.Id] = new List<MeshEdge>();
        }
    }

    /// <summary>Adds an undirected link. Duplicate links and self-loops are ignored.</summary>
    public bool AddEdge(string a, string b, double cost)
    {
        if (a == b || !_vertices.ContainsKey(a) || !_vertices.ContainsKey(b)) return false;
        if (_adjacency[a].Any(e => e.Other(a) == b)) return false;

        var edge = new MeshEdge(a, b, cost);
        _adjacency[a].Add(edge);
        _adjacency[b].Add(edge);
        _edges.Add(edge);
        return true;
    }

    public IReadOnlyList<MeshEdge> LinksOf(string id) =>
        _adjacency.TryGetValue(id, out var list) ? list : Array.Empty<MeshEdge>();

    public IEnumerable<string> NeighboursOf(string id) => LinksOf(id).Select(e => e.Other(id));

    // ------------------------------------------------------------- traversal

    /// <summary>
    /// Breadth-first search: visits vertices in order of hop distance from
    /// <paramref name="start"/>, using a FIFO queue. Finds every device
    /// still reachable from a gateway, and the fewest-hop route to each.
    /// </summary>
    public TraversalResult Bfs(string start, Func<string, bool>? passable = null)
    {
        var order = new List<string>();
        var parent = new Dictionary<string, string?>();
        if (!Contains(start) || !(passable?.Invoke(start) ?? true))
        {
            return new TraversalResult(order, parent);
        }

        var queue = new Queue<string>();
        parent[start] = null;
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            string current = queue.Dequeue();
            order.Add(current);

            foreach (string next in NeighboursOf(current))
            {
                if (parent.ContainsKey(next)) continue;
                if (!(passable?.Invoke(next) ?? true)) continue;
                parent[next] = current;
                queue.Enqueue(next);
            }
        }

        return new TraversalResult(order, parent);
    }

    /// <summary>
    /// Depth-first search (iterative, explicit stack): follows one branch of
    /// the mesh as deep as it goes before backtracking.
    /// </summary>
    public TraversalResult Dfs(string start, Func<string, bool>? passable = null)
    {
        var order = new List<string>();
        var parent = new Dictionary<string, string?>();
        if (!Contains(start) || !(passable?.Invoke(start) ?? true))
        {
            return new TraversalResult(order, parent);
        }

        var visited = new HashSet<string>();
        var stack = new Stack<(string Id, string? Via)>();
        stack.Push((start, null));

        while (stack.Count > 0)
        {
            var (current, via) = stack.Pop();
            if (!visited.Add(current)) continue;

            parent[current] = via;
            order.Add(current);

            // Push in reverse so neighbours are explored in their stored order.
            var neighbours = NeighboursOf(current).Where(n => !visited.Contains(n) && (passable?.Invoke(n) ?? true)).ToList();
            for (int i = neighbours.Count - 1; i >= 0; i--)
            {
                stack.Push((neighbours[i], current));
            }
        }

        return new TraversalResult(order, parent);
    }

    /// <summary>Fewest-hop path (BFS), or null when <paramref name="target"/> cannot be reached.</summary>
    public List<string>? ShortestHopPath(string source, string target, Func<string, bool>? passable = null)
    {
        var bfs = Bfs(source, passable);
        return PathFromParents(bfs.Parent, target);
    }

    /// <summary>Rebuilds the root-to-target path from a traversal's parent map.</summary>
    public static List<string>? PathFromParents(IReadOnlyDictionary<string, string?> parent, string target)
    {
        if (!parent.ContainsKey(target)) return null;

        var path = new List<string>();
        string? current = target;
        while (current is not null)
        {
            path.Add(current);
            current = parent[current];
        }
        path.Reverse();
        return path;
    }

    // ---------------------------------------------------------- spanning tree

    /// <summary>
    /// Prim's algorithm. Grows the tree outwards from <paramref name="root"/>
    /// (the central server): a <see cref="MinHeap{TElement,TPriority}"/> of
    /// candidate links always yields the cheapest link that reaches a vertex
    /// not yet in the tree. The result is the set of links that connects every
    /// reachable node to the server at the lowest total power cost.
    /// </summary>
    public SpanningTreeResult Prim(string root, Func<string, bool>? passable = null)
    {
        var edges = new List<MeshEdge>();
        if (!Contains(root) || !(passable?.Invoke(root) ?? true))
        {
            return new SpanningTreeResult(edges, 0, 0);
        }

        var inTree = new HashSet<string> { root };
        var heap = new MinHeap<(MeshEdge Edge, string To), double>();
        double total = 0;

        void PushLinks(string origin)
        {
            foreach (var link in LinksOf(origin))
            {
                string other = link.Other(origin);
                if (inTree.Contains(other)) continue;
                if (!(passable?.Invoke(other) ?? true)) continue;
                heap.Push((link, other), link.Cost);
            }
        }

        PushLinks(root);

        while (heap.TryPop(out var item, out _))
        {
            if (inTree.Contains(item.To)) continue; // stale entry: already reached more cheaply

            inTree.Add(item.To);
            edges.Add(item.Edge);
            total += item.Edge.Cost;
            PushLinks(item.To);
        }

        return new SpanningTreeResult(edges, Math.Round(total, 2), inTree.Count);
    }

    /// <summary>
    /// Kruskal's algorithm (sort edges, union-find to reject cycles). Not used
    /// by the UI - it exists as an independent second implementation so the
    /// test suite can confirm Prim's total cost is truly minimal.
    /// </summary>
    public SpanningTreeResult Kruskal(Func<string, bool>? passable = null)
    {
        var usable = _vertices.Keys.Where(id => passable?.Invoke(id) ?? true).ToList();
        var parent = usable.ToDictionary(id => id, id => id);

        string Find(string x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]]; // path halving
                x = parent[x];
            }
            return x;
        }

        var chosen = new List<MeshEdge>();
        double total = 0;

        foreach (var edge in _edges
                     .Where(e => parent.ContainsKey(e.A) && parent.ContainsKey(e.B))
                     .OrderBy(e => e.Cost))
        {
            string ra = Find(edge.A), rb = Find(edge.B);
            if (ra == rb) continue;
            parent[ra] = rb;
            chosen.Add(edge);
            total += edge.Cost;
        }

        return new SpanningTreeResult(chosen, Math.Round(total, 2), usable.Count);
    }

    /// <summary>
    /// Route from <paramref name="source"/> to <paramref name="target"/> along the
    /// given spanning tree's links only (the power-efficient routing path).
    /// </summary>
    public static List<string>? PathInTree(SpanningTreeResult tree, string source, string target)
    {
        var adjacency = new Dictionary<string, List<string>>();
        foreach (var e in tree.Edges)
        {
            if (!adjacency.TryGetValue(e.A, out var la)) adjacency[e.A] = la = new List<string>();
            if (!adjacency.TryGetValue(e.B, out var lb)) adjacency[e.B] = lb = new List<string>();
            la.Add(e.B);
            lb.Add(e.A);
        }

        if (source == target) return new List<string> { source };
        if (!adjacency.ContainsKey(source) || !adjacency.ContainsKey(target)) return null;

        var parent = new Dictionary<string, string?> { [source] = null };
        var queue = new Queue<string>();
        queue.Enqueue(source);
        while (queue.Count > 0)
        {
            string current = queue.Dequeue();
            foreach (string next in adjacency[current])
            {
                if (parent.ContainsKey(next)) continue;
                parent[next] = current;
                queue.Enqueue(next);
            }
        }

        return PathFromParents(parent, target);
    }

    /// <summary>Sum of the link costs along a vertex path.</summary>
    public double PathCost(IReadOnlyList<string> path)
    {
        double total = 0;
        for (int i = 0; i < path.Count - 1; i++)
        {
            var link = LinksOf(path[i]).FirstOrDefault(e => e.Other(path[i]) == path[i + 1]);
            if (link is not null) total += link.Cost;
        }
        return Math.Round(total, 2);
    }
}
