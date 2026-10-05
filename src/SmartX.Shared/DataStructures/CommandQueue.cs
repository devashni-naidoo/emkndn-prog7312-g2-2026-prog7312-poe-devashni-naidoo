using SmartX.Shared.Commands;

namespace SmartX.Shared.DataStructures;

/// <summary>
/// Standard First-In-First-Out processing of incoming device commands,
/// backed directly by <see cref="Queue{T}"/>. Not thread-safe by itself —
/// callers (the API's background processor) are responsible for
/// synchronising access, since a bare <c>Queue&lt;T&gt;</c> is what the
/// brief specifically asks for rather than a concurrent collection.
/// </summary>
public sealed class CommandQueue
{
    private readonly Queue<DeviceCommand> _queue = new();

    public int Count => _queue.Count;

    public void Enqueue(DeviceCommand command) => _queue.Enqueue(command);

    public bool TryDequeue(out DeviceCommand? command) => _queue.TryDequeue(out command);

    public IReadOnlyCollection<DeviceCommand> Snapshot() => _queue.ToArray();
}
