using SmartX.Shared.Commands;

namespace SmartX.Shared.DataStructures;

/// <summary>
/// A severe power spike or a critical moisture drop must bypass the
/// standard <see cref="CommandQueue"/> and be processed immediately. This
/// wraps <see cref="PriorityQueue{TElement,TPriority}"/>, ordering by
/// enqueue time (ticks) so that within the critical tier, commands still
/// process oldest-first — the priority only jumps the whole critical batch
/// ahead of standard-severity work, it doesn't reorder critical commands
/// against each other.
/// </summary>
public sealed class CriticalAlertQueue
{
    private readonly PriorityQueue<DeviceCommand, long> _queue = new();

    public int Count => _queue.Count;

    public void Enqueue(DeviceCommand command) => _queue.Enqueue(command, command.IssuedUtc.UtcTicks);

    public bool TryDequeue(out DeviceCommand? command) => _queue.TryDequeue(out command, out _);

    public bool TryPeek(out DeviceCommand? command) => _queue.TryPeek(out command, out _);
}
