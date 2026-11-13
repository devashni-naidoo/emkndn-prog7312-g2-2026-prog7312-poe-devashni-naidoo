using Microsoft.AspNetCore.SignalR;
using SmartX.Api.Hubs;
using SmartX.Shared.Commands;
using SmartX.Shared.DataStructures;

namespace SmartX.Api.Services;

public sealed class CommandProcessorWorker : BackgroundService
{
    private readonly CommandQueue _standard = new();
    private readonly CriticalAlertQueue _critical = new();
    private readonly CommandHistoryStack _history = new();

    private readonly DeviceStateService _state;
    private readonly AlertCoordinator _alerts;
    private readonly IHubContext<CommandHub> _hub;
    private readonly ILogger<CommandProcessorWorker> _logger;

    public CommandProcessorWorker(
        DeviceStateService state, AlertCoordinator alerts, IHubContext<CommandHub> hub,
        ILogger<CommandProcessorWorker> logger)
    {
        _state = state;
        _alerts = alerts;
        _hub = hub;
        _logger = logger;
    }

    public int StandardQueueDepth => _standard.Count;
    public int CriticalQueueDepth => _critical.Count;

    public void Enqueue(DeviceCommand command)
    {
        if (command.Severity == CommandSeverity.Critical)
        {
            _critical.Enqueue(command);
        }
        else
        {
            _standard.Enqueue(command);
        }
    }


    public DeviceCommand? UndoLast()
    {
        var inverse = _history.Undo();
        if (inverse is not null)
        {
            Enqueue(inverse);
        }
        return inverse;
    }

    public IReadOnlyList<DeviceCommand> RecentHistory(int take = 20) => _history.Snapshot(take);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Critical alerts always win the race against standard-severity
                // work — that's the entire point of the priority queue.
                DeviceCommand? next;
                bool wasCritical;

                if (_critical.TryDequeue(out next) && next is not null)
                {
                    wasCritical = true;
                }
                else if (_standard.TryDequeue(out next) && next is not null)
                {
                    wasCritical = false;
                }
                else
                {
                    await Task.Delay(150, stoppingToken);
                    continue;
                }

                string outcome = _state.Apply(next);
                _history.Push(next);
                _alerts.RecordCommandAgainstActiveAlerts(next.SensorId, next.Type);

                string kind = next.IsUndo ? " (undo)" : "";
                await _alerts.LogAsync(next.SensorId, SmartX.Shared.Commands.LogLevel.Info, $"{next.Type} executed: {outcome}{kind}");

                var result = new CommandResult
                {
                    CommandId = next.Id,
                    SensorId = next.SensorId,
                    Type = next.Type,
                    Success = true,
                    Message = outcome,
                    WasCritical = wasCritical
                };

                await _hub.Clients.All.SendAsync("ReceiveCommandResult", result, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on host shutdown.
        }
    }
}
