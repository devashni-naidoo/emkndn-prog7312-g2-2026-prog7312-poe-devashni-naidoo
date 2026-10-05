using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;
using SmartX.Shared.Dtos;
using SmartX.Shared.Telemetry;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/telemetry")]
public sealed class TelemetryController : ControllerBase
{
    private readonly TelemetryStore _store;
    private readonly ILogger<TelemetryController> _logger;

    public TelemetryController(TelemetryStore store, ILogger<TelemetryController> logger)
    {
        _store = store;
        _logger = logger;
    }

    /// <summary>Ingests a single reading and returns the recomputed snapshot.</summary>
    [HttpPost]
    public async Task<ActionResult<TelemetrySnapshotDto>> Ingest([FromBody] IngestReadingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SensorId))
        {
            return BadRequest(new { error = "SensorId is required." });
        }

        var snapshot = await _store.IngestAsync(request.SensorId, request.Value);
        return Ok(snapshot);
    }

    /// <summary>
    /// Ingests a burst of readings as one jagged-array batch — exercises
    /// <see cref="TelemetryHistoryBuffer{T}"/> directly.
    /// </summary>
    [HttpPost("batch")]
    public async Task<ActionResult<object>> IngestBatch([FromBody] IngestBatchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SensorId))
        {
            return BadRequest(new { error = "SensorId is required." });
        }

        int count = _store.IngestBatch(request.SensorId, request.Values);
        _logger.LogInformation("Ingested batch of {Count} readings for {SensorId}", count, request.SensorId);
        var snapshot = await _store.GetSnapshotAsync(request.SensorId);
        return Ok(new { ingested = count, snapshot });
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TelemetrySnapshotDto>>> GetAll() => Ok(await _store.GetAllSnapshotsAsync());

    [HttpGet("{sensorId}")]
    public async Task<ActionResult<TelemetrySnapshotDto>> GetOne(string sensorId)
    {
        var snapshot = await _store.GetSnapshotAsync(sensorId);
        return snapshot is null ? NotFound() : Ok(snapshot);
    }

    /// <summary>Raw jagged-array batches currently buffered for a sensor (diagnostics/demo).</summary>
    [HttpGet("{sensorId}/raw-batches")]
    public ActionResult<double[][]> GetRawBatches(string sensorId)
    {
        var batches = _store.GetRawBatches(sensorId);
        return batches is null ? NotFound() : Ok(batches);
    }

    /// <summary>
    /// Demonstrates the overloaded <c>+</c> operator on
    /// <see cref="PowerMeterReading"/>: Meter3 = Meter1 + Meter2.
    /// </summary>
    [HttpPost("aggregate")]
    public async Task<ActionResult<object>> AggregateMeters([FromBody] AggregateMetersRequest request)
    {
        var a = await _store.GetSnapshotAsync(request.MeterIdA);
        var b = await _store.GetSnapshotAsync(request.MeterIdB);
        if (a is null || b is null)
        {
            return NotFound(new { error = "Both meter IDs must have at least one ingested reading." });
        }

        var meter1 = new PowerMeterReading(request.MeterIdA, a.LatestValue);
        var meter2 = new PowerMeterReading(request.MeterIdB, b.LatestValue);

        // The actual operator-overload usage the brief asks for:
        PowerMeterReading meter3 = meter1 + meter2;
        PowerMeterReading delta = meter1 - meter2;

        return Ok(new
        {
            meter1 = new { meter1.MeterId, meter1.Watts },
            meter2 = new { meter2.MeterId, meter2.Watts },
            aggregate = new { meter3.MeterId, meter3.Watts },
            delta = new { delta.MeterId, delta.Watts },
            meter1IsGreater = meter1 > meter2
        });
    }
}
