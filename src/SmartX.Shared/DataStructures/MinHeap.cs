namespace SmartX.Shared.DataStructures;

public sealed class MinHeap<TElement, TPriority> where TPriority : IComparable<TPriority>
{
    private readonly List<(TElement Element, TPriority Priority)> _items = new();

    public int Count => _items.Count;

    public void Push(TElement element, TPriority priority)
    {
        _items.Add((element, priority));
        SiftUp(_items.Count - 1);
    }

    public TElement Peek()
    {
        if (_items.Count == 0) throw new InvalidOperationException("Heap is empty.");
        return _items[0].Element;
    }

    public bool TryPop(out TElement? element, out TPriority? priority)
    {
        if (_items.Count == 0)
        {
            element = default;
            priority = default;
            return false;
        }

        var top = _items[0];
        int last = _items.Count - 1;
        _items[0] = _items[last];
        _items.RemoveAt(last);
        if (_items.Count > 0) SiftDown(0);

        element = top.Element;
        priority = top.Priority;
        return true;
    }

    private void SiftUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (_items[index].Priority.CompareTo(_items[parent].Priority) >= 0) break;
            (_items[index], _items[parent]) = (_items[parent], _items[index]);
            index = parent;
        }
    }

    private void SiftDown(int index)
    {
        int count = _items.Count;
        while (true)
        {
            int left = 2 * index + 1;
            int right = left + 1;
            int smallest = index;

            if (left < count && _items[left].Priority.CompareTo(_items[smallest].Priority) < 0) smallest = left;
            if (right < count && _items[right].Priority.CompareTo(_items[smallest].Priority) < 0) smallest = right;
            if (smallest == index) break;

            (_items[index], _items[smallest]) = (_items[smallest], _items[index]);
            index = smallest;
        }
    }
}

/*
 * Title:        Introduction to Algorithms (binary heap: sift-up and sift-down)
 * Author:       Cormen, T.H., Leiserson, C.E., Rivest, R.L. and Stein, C.
 * Version:      4th edition (2022)
 * Date:         2022
 * Availability: https://mitpress.mit.edu/9780262046305/introduction-to-algorithms/
 */