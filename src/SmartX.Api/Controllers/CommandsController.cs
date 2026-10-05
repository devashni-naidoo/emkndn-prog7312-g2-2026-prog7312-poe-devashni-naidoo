using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;
using SmartX.Shared.Commands;
using SmartX.Shared.Dtos;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/commands")]
public sealed class CommandsController : ControllerBase
{
    private readonly CommandProcessorWorker _worker;
    private readonly SensorRegistry _registry;
    private readonly AlertCoordinator _alerts;

    public CommandsController(CommandProcessorWorker worker, SensorRegistry registry, AlertCoordinator alerts)
    {
        _worker = worker;
        _registry = registry;
        _alerts = alerts;
    }

    /// <summary>
    /// Issues a manual override command. Critical-severity commands are
    /// routed to the priority queue and jump ahead of whatever standard
    /// commands are already waiting; everything else joins the standard
    /// FIFO queue. The <see cref="CommandProcessorWorker"/> background
    /// service drains both continuously, so this returns immediately with
    /// 202 Accepted — the actual result arrives on the dashboard via
    /// SignalR once it's processed.
    /// </summary>
    [HttpPost]
    public IActionResult Issue([FromBody] IssueCommandRequest request)
    {
        var profile = _registry.Get(request.SensorId);
        if (profile is null)
        {
            return NotFound(new { error = $"No sensor registered with id '{request.SensorId}'." });
        }

        var command = new DeviceCommand
        {
            SensorId = request.SensorId,
            Type = request.Type,
            Severity = request.Severity,
            Payload = request.Payload
        };

        _worker.Enqueue(command);

        return Accepted(new { commandId = command.Id, queuedAs = request.Severity.ToString() });
    }

    /// <summary>Reverts the most recently executed command by re-queueing its inverse.</summary>
    [HttpPost("undo")]
    public IActionResult Undo()
    {
        var inverse = _worker.UndoLast();
        if (inverse is null)
        {
            return NotFound(new { error = "Nothing to undo." });
        }
        return Accepted(new { commandId = inverse.Id, type = inverse.Type.ToString(), sensorId = inverse.SensorId });
    }

    /// <summary>Most recent commands actually executed, newest first (off the undo stack).</summary>
    [HttpGet("history")]
    public ActionResult<IReadOnlyList<DeviceCommand>> RecentHistory([FromQuery] int take = 20) =>
        Ok(_worker.RecentHistory(take));

    /// <summary>Timestamp-ordered timeline entries for one sensor (backed by SortedDictionary).</summary>
    [HttpGet("timeline/{sensorId}")]
    public ActionResult<IReadOnlyList<LogEntryDto>> Timeline(string sensorId, [FromQuery] int take = 50)
    {
        var entries = _alerts.Timeline.ForSensor(sensorId, take)
            .Select(e => new LogEntryDto
            {
                SensorId = e.SensorId,
                TimestampUtc = e.TimestampUtc,
                Level = e.Level,
                Message = e.Message
            })
            .ToList();
        return Ok(entries);
    }

    [HttpGet("queue-depth")]
    public ActionResult<object> QueueDepth() => Ok(new
    {
        standard = _worker.StandardQueueDepth,
        critical = _worker.CriticalQueueDepth
    });
}
