using SmartX.Shared.Commands;

namespace SmartX.Shared.DataStructures;

/// <summary>
/// History of manual override commands actually sent to devices, backed by
/// <see cref="Stack{T}"/> so the most recently issued command is always
/// what Undo reverts — e.g. reverting a "shut off all valves" action re-opens
/// them, without having to search through a list for "the last command".
/// </summary>
public sealed class CommandHistoryStack
{
    private readonly Stack<DeviceCommand> _history = new();

    public int Count => _history.Count;

    public void Push(DeviceCommand command) => _history.Push(command);

    /// <summary>
    /// Pops the most recent command and returns its inverse, ready to be
    /// re-issued through the queue — or <c>null</c> if there's nothing to
    /// undo, or the most recent command has no meaningful inverse (e.g. a
    /// sensor reset).
    /// </summary>
    public DeviceCommand? Undo()
    {
        if (!_history.TryPop(out var last))
        {
            return null;
        }
        return last.Inverse();
    }

    public DeviceCommand? PeekLast() => _history.TryPeek(out var last) ? last : null;

    /// <summary>Most-recent-first snapshot for display, without mutating the stack.</summary>
    public IReadOnlyList<DeviceCommand> Snapshot(int take = 20) => _history.Take(take).ToList();
}
