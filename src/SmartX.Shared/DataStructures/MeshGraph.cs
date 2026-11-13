namespace SmartX.Shared.DataStructures;

public enum MeshNodeKind
{
    Server,
    Gateway,
    Sensor
}

public sealed record MeshVertex(string Id, string Label, MeshNodeKind Kind, string Zone);


public sealed record MeshEdge(string A, string B, double Cost)
{
    public string Other(string id) => id == A ? B : A;
}


public sealed record TraversalResult(
    IReadOnlyList<string> VisitOrder,
    IReadOnlyDictionary<string, string?> Parent);


public sealed record SpanningTreeResult(IReadOnlyList<MeshEdge> Edges, double TotalCost, int VerticesSpanned);

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


            var neighbours = NeighboursOf(current).Where(n => !visited.Contains(n) && (passable?.Invoke(n) ?? true)).ToList();
            for (int i = neighbours.Count - 1; i >= 0; i--)
            {
                stack.Push((neighbours[i], current));
            }
        }

        return new TraversalResult(order, parent);
    }

    public List<string>? ShortestHopPath(string source, string target, Func<string, bool>? passable = null)
    {
        var bfs = Bfs(source, passable);
        return PathFromParents(bfs.Parent, target);
    }


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

    /// Prim's algorithm
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
            if (inTree.Contains(item.To)) continue; 

            inTree.Add(item.To);
            edges.Add(item.Edge);
            total += item.Edge.Cost;
            PushLinks(item.To);
        }

        return new SpanningTreeResult(edges, Math.Round(total, 2), inTree.Count);
    }

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

/*
 * Title:        Introduction to Algorithms (BFS, DFS, Prim's and Kruskal's minimum spanning tree)
 * Author:       Cormen, T.H., Leiserson, C.E., Rivest, R.L. and Stein, C.
 * Version:      4th edition (2022)
 * Date:         2022
 * Availability: https://mitpress.mit.edu/9780262046305/introduction-to-algorithms/
 */