using SmartX.Shared.Commands;

namespace SmartX.Shared.DataStructures;


/// Standard First-In-First-Out processing of incoming device commands,
/// backed directly by <see cref="Queue{T}"/>. 
public sealed class CommandQueue
{
    private readonly Queue<DeviceCommand> _queue = new();

    public int Count => _queue.Count;

    public void Enqueue(DeviceCommand command) => _queue.Enqueue(command);

    public bool TryDequeue(out DeviceCommand? command) => _queue.TryDequeue(out command);

    public IReadOnlyCollection<DeviceCommand> Snapshot() => _queue.ToArray();
}
