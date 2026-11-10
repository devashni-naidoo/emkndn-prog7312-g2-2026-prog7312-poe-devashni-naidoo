namespace SmartX.Shared.DataStructures;

/// <summary>
/// Self-balancing AVL binary search tree (Part 3 - Trees requirement).
/// Every node stores its subtree height; after each insert the balance
/// factor (left height - right height) of every node on the insertion path
/// is checked and, if it leaves the range [-1, 1], one of the four rotations
/// (LL, RR, LR, RL) restores it. The tree height is therefore always
/// O(log n), which is what makes <see cref="Range"/> queries O(log n + k)
/// (k = number of matches) instead of an O(n) scan of an unordered list.
/// </summary>
/// <typeparam name="TKey">Ordering key (must be comparable).</typeparam>
/// <typeparam name="TValue">Payload stored against each key.</typeparam>
public sealed class AvlTree<TKey, TValue> where TKey : IComparable<TKey>
{
    private sealed class Node
    {
        public TKey Key;
        public TValue Value;
        public int Height = 1;
        public Node? Left;
        public Node? Right;

        public Node(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }

    private Node? _root;

    public int Count { get; private set; }

    /// <summary>Number of rotations performed so far (useful for demos/tests).</summary>
    public int RotationCount { get; private set; }

    /// <summary>Height of the tree (0 when empty, 1 for a single node).</summary>
    public int Height => HeightOf(_root);

    /// <summary>Inserts a key, replacing the value if the key already exists.</summary>
    public void Insert(TKey key, TValue value)
    {
        _root = Insert(_root, key, value);
    }

    public bool TryGet(TKey key, out TValue? value)
    {
        var node = _root;
        while (node is not null)
        {
            int cmp = key.CompareTo(node.Key);
            if (cmp == 0)
            {
                value = node.Value;
                return true;
            }
            node = cmp < 0 ? node.Left : node.Right;
        }
        value = default;
        return false;
    }

    public bool TryGetMin(out TKey? key, out TValue? value)
    {
        var node = _root;
        if (node is null) { key = default; value = default; return false; }
        while (node.Left is not null) node = node.Left;
        key = node.Key;
        value = node.Value;
        return true;
    }

    public bool TryGetMax(out TKey? key, out TValue? value)
    {
        var node = _root;
        if (node is null) { key = default; value = default; return false; }
        while (node.Right is not null) node = node.Right;
        key = node.Key;
        value = node.Value;
        return true;
    }

    /// <summary>
    /// Returns every value whose key lies in the inclusive range
    /// [<paramref name="low"/>, <paramref name="high"/>], in ascending key
    /// order. Whole subtrees that cannot contain a match are skipped, so
    /// only O(log n + k) nodes are ever visited.
    /// </summary>
    /// <param name="nodesVisited">How many tree nodes the query touched.</param>
    public List<TValue> Range(TKey low, TKey high, out int nodesVisited)
    {
        var results = new List<TValue>();
        int visited = 0;
        if (low.CompareTo(high) <= 0)
        {
            CollectRange(_root, low, high, results, ref visited);
        }
        nodesVisited = visited;
        return results;
    }

    /// <summary>All values in ascending key order (in-order traversal).</summary>
    public List<TValue> InOrder()
    {
        var results = new List<TValue>(Count);
        var stack = new Stack<Node>();
        var current = _root;
        while (current is not null || stack.Count > 0)
        {
            while (current is not null)
            {
                stack.Push(current);
                current = current.Left;
            }
            current = stack.Pop();
            results.Add(current.Value);
            current = current.Right;
        }
        return results;
    }

    /// <summary>
    /// Verifies the AVL invariants (BST ordering, correct stored heights,
    /// every balance factor within [-1, 1]). Used by the test suite.
    /// </summary>
    public bool IsValidAvl() => Validate(_root, false, default!, false, default!, out _);

    // ------------------------------------------------------------------ internals

    private static int HeightOf(Node? node) => node?.Height ?? 0;

    private static void UpdateHeight(Node node) =>
        node.Height = 1 + Math.Max(HeightOf(node.Left), HeightOf(node.Right));

    private static int BalanceOf(Node node) => HeightOf(node.Left) - HeightOf(node.Right);

    private Node Insert(Node? node, TKey key, TValue value)
    {
        if (node is null)
        {
            Count++;
            return new Node(key, value);
        }

        int cmp = key.CompareTo(node.Key);
        if (cmp < 0) node.Left = Insert(node.Left, key, value);
        else if (cmp > 0) node.Right = Insert(node.Right, key, value);
        else
        {
            node.Value = value;
            return node;
        }

        return Rebalance(node);
    }

    private Node Rebalance(Node node)
    {
        UpdateHeight(node);
        int balance = BalanceOf(node);

        if (balance > 1)
        {
            // Left-heavy. If the left child leans right, rotate it left first (LR case).
            if (BalanceOf(node.Left!) < 0)
            {
                node.Left = RotateLeft(node.Left!);
            }
            return RotateRight(node);
        }

        if (balance < -1)
        {
            // Right-heavy. If the right child leans left, rotate it right first (RL case).
            if (BalanceOf(node.Right!) > 0)
            {
                node.Right = RotateRight(node.Right!);
            }
            return RotateLeft(node);
        }

        return node;
    }

    private Node RotateRight(Node y)
    {
        var x = y.Left!;
        y.Left = x.Right;
        x.Right = y;
        UpdateHeight(y);
        UpdateHeight(x);
        RotationCount++;
        return x;
    }

    private Node RotateLeft(Node x)
    {
        var y = x.Right!;
        x.Right = y.Left;
        y.Left = x;
        UpdateHeight(x);
        UpdateHeight(y);
        RotationCount++;
        return y;
    }

    private static void CollectRange(Node? node, TKey low, TKey high, List<TValue> results, ref int visited)
    {
        if (node is null) return;
        visited++;

        // Only descend left if there could be keys >= low down there.
        if (low.CompareTo(node.Key) < 0)
        {
            CollectRange(node.Left, low, high, results, ref visited);
        }

        if (low.CompareTo(node.Key) <= 0 && node.Key.CompareTo(high) <= 0)
        {
            results.Add(node.Value);
        }

        // Only descend right if there could be keys <= high down there.
        if (high.CompareTo(node.Key) > 0)
        {
            CollectRange(node.Right, low, high, results, ref visited);
        }
    }

    private static bool Validate(Node? node, bool hasMin, TKey min, bool hasMax, TKey max, out int height)
    {
        if (node is null)
        {
            height = 0;
            return true;
        }

        if (hasMin && node.Key.CompareTo(min) <= 0) { height = 0; return false; }
        if (hasMax && node.Key.CompareTo(max) >= 0) { height = 0; return false; }

        if (!Validate(node.Left, hasMin, min, true, node.Key, out int lh) ||
            !Validate(node.Right, true, node.Key, hasMax, max, out int rh))
        {
            height = 0;
            return false;
        }

        height = 1 + Math.Max(lh, rh);
        return height == node.Height && Math.Abs(lh - rh) <= 1;
    }
}
