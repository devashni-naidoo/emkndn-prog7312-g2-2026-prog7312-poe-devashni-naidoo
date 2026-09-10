using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;
using SmartX.Shared.Dtos;
using SmartX.Shared.Models;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/sensors")]
public sealed class SensorsController : ControllerBase
{
    private readonly SensorRegistry _registry;
    private readonly SensorSimulator _simulator;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<SensorsController> _logger;

    // 10 MB cap on any single attached config file / photo / log.
    private const long MaxAttachmentBytes = 10 * 1024 * 1024;

    public SensorsController(
        SensorRegistry registry,
        SensorSimulator simulator,
        IWebHostEnvironment env,
        ILogger<SensorsController> logger)
    {
        _registry = registry;
        _simulator = simulator;
        _env = env;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<IReadOnlyCollection<DeviceProfile>> GetAll() => Ok(_registry.GetAll());

    [HttpGet("{id}")]
    public ActionResult<DeviceProfile> GetById(string id)
    {
        var profile = _registry.Get(id);
        return profile is null ? NotFound() : Ok(profile);
    }

    /// <summary>
    /// Registers a new (simulated or attached) sensor. The deployment path
    /// is validated recursively server-side before anything is stored — see
    /// <see cref="SmartX.Shared.Validation.DeploymentValidator"/>.
    /// </summary>
    [HttpPost]
    public ActionResult<DeviceProfile> Register([FromBody] RegisterSensorRequest request)
    {
        var (success, message, profile) = _registry.Register(
            request.MacAddress, request.Facility, request.Zone,
            request.SubZone, request.Room, request.NodeId, request.Category);

        if (!success)
        {
            return BadRequest(new { error = message });
        }

        _logger.LogInformation("Registered sensor {SensorId}: {Message}", profile!.Id, message);
        return CreatedAtAction(nameof(GetById), new { id = profile.Id }, profile);
    }

    /// <summary>
    /// Optimised multipart file upload for device configuration files,
    /// deployment photos, or hardware logs attached to a sensor profile.
    /// Streams straight to disk rather than buffering the whole file in
    /// memory, and rejects anything over <see cref="MaxAttachmentBytes"/>
    /// before it is written.
    /// </summary>
    [HttpPost("{id}/media")]
    [RequestSizeLimit(MaxAttachmentBytes)]
    public async Task<IActionResult> UploadMedia(string id, IFormFile file)
    {
        var profile = _registry.Get(id);
        if (profile is null) return NotFound(new { error = $"No sensor registered with id '{id}'." });

        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "No file was uploaded." });
        }
        if (file.Length > MaxAttachmentBytes)
        {
            return BadRequest(new { error = "File exceeds the 10 MB attachment limit." });
        }

        var uploadsRoot = Path.Combine(_env.ContentRootPath, "App_Data", "uploads", id);
        Directory.CreateDirectory(uploadsRoot);

        var safeFileName = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss}_{Path.GetFileName(file.FileName)}";
        var fullPath = Path.Combine(uploadsRoot, safeFileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var attachment = new MediaAttachment
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            StoragePath = fullPath
        };

        _registry.AddAttachment(id, attachment);
        _logger.LogInformation("Stored attachment {FileName} ({Size} bytes) for sensor {SensorId}",
            file.FileName, file.Length, id);

        return Ok(attachment);
    }

    [HttpPost("{id}/simulate/start")]
    public IActionResult StartSimulation(string id)
    {
        if (!_simulator.Start(id))
        {
            return NotFound(new { error = $"No sensor registered with id '{id}'." });
        }
        return Ok(new { running = true });
    }

    [HttpPost("{id}/simulate/stop")]
    public IActionResult StopSimulation(string id)
    {
        _simulator.Stop(id);
        return Ok(new { running = false });
    }

    [HttpGet("{id}/simulate/status")]
    public ActionResult<object> SimulationStatus(string id) => Ok(new { running = _simulator.IsRunning(id) });
}
