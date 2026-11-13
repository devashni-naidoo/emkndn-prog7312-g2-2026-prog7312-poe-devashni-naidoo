using SmartX.Shared.Commands;

namespace SmartX.Shared.DataStructures;

public sealed class CriticalAlertQueue
{
    private readonly PriorityQueue<DeviceCommand, long> _queue = new();

    public int Count => _queue.Count;

    public void Enqueue(DeviceCommand command) => _queue.Enqueue(command, command.IssuedUtc.UtcTicks);

    public bool TryDequeue(out DeviceCommand? command) => _queue.TryDequeue(out command, out _);

    public bool TryPeek(out DeviceCommand? command) => _queue.TryPeek(out command, out _);
}
