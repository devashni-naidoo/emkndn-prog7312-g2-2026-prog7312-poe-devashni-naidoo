using SmartX.Shared.Commands;

namespace SmartX.Shared.DataStructures;

public sealed class CriticalAlertQueue
{
    private readonly PriorityQueue<DeviceCommand, long> _queue = new();
    private readonly object _gate = new();

    public int Count { get { lock (_gate) return _queue.Count; } }

    public void Enqueue(DeviceCommand command) { lock (_gate) _queue.Enqueue(command, command.IssuedUtc.UtcTicks); }

    public bool TryDequeue(out DeviceCommand? command) { lock (_gate) return _queue.TryDequeue(out command, out _); }

    public bool TryPeek(out DeviceCommand? command) { lock (_gate) return _queue.TryPeek(out command, out _); }
}
