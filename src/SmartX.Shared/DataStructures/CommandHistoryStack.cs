using SmartX.Shared.Commands;

namespace SmartX.Shared.DataStructures;

public sealed class CommandHistoryStack
{
    private readonly Stack<DeviceCommand> _history = new();
    private readonly object _gate = new();

    public int Count { get { lock (_gate) return _history.Count; } }

    public void Push(DeviceCommand command) { lock (_gate) _history.Push(command); }

    public DeviceCommand? Undo()
    {
        DeviceCommand? last;
        lock (_gate)
        {
            if (!_history.TryPop(out last))
            {
                return null;
            }
        }
        return last.Inverse();
    }

    public DeviceCommand? PeekLast() { lock (_gate) return _history.TryPeek(out var last) ? last : null; }

    public IReadOnlyList<DeviceCommand> Snapshot(int take = 20) { lock (_gate) return _history.Take(take).ToList(); }
}
