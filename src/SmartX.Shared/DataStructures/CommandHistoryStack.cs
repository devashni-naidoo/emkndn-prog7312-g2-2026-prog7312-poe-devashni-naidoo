using SmartX.Shared.Commands;

namespace SmartX.Shared.DataStructures;

public sealed class CommandHistoryStack
{
    private readonly Stack<DeviceCommand> _history = new();

    public int Count => _history.Count;

    public void Push(DeviceCommand command) => _history.Push(command);

    /// Pops the most recent command and returns its inverse, ready to be
    /// re-issued through the queue — or <c>null</c> if there's nothing to
    /// undo, or the most recent command has no meaningful inverse
    public DeviceCommand? Undo()
    {
        if (!_history.TryPop(out var last))
        {
            return null;
        }
        return last.Inverse();
    }

    public DeviceCommand? PeekLast() => _history.TryPeek(out var last) ? last : null;

    public IReadOnlyList<DeviceCommand> Snapshot(int take = 20) => _history.Take(take).ToList();
}
