using SmartX.Shared.Commands;

namespace SmartX.Shared.DataStructures;

public sealed class CommandQueue
{
    private readonly Queue<DeviceCommand> _queue = new();
    private readonly object _gate = new();

    public int Count { get { lock (_gate) return _queue.Count; } }

    public void Enqueue(DeviceCommand command) { lock (_gate) _queue.Enqueue(command); }

    public bool TryDequeue(out DeviceCommand? command) { lock (_gate) return _queue.TryDequeue(out command); }

    public IReadOnlyCollection<DeviceCommand> Snapshot() { lock (_gate) return _queue.ToArray(); }
}
