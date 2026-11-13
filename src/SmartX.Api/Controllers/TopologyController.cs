using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;
using SmartX.Shared.Dtos;

namespace SmartX.Api.Controllers;


[ApiController]
[Route("api/topology")]
public sealed class TopologyController : ControllerBase
{
    private readonly TopologyService _topology;

    public TopologyController(TopologyService topology)
    {
        _topology = topology;
    }


    [HttpGet]
    public async Task<ActionResult<TopologyDto>> Get() => Ok(await _topology.GetTopologyAsync());

    [HttpGet("route")]
    public async Task<ActionResult<RouteDto>> Route([FromQuery] string? id, [FromQuery] string? mode)
    {
        var route = await _topology.GetRouteAsync(id, mode);
        return route.NodeId.Length == 0 ? NotFound(new { error = route.Message }) : Ok(route);
    }

    [HttpGet("connectivity")]
    public async Task<ActionResult<ConnectivityDto>> Connectivity([FromQuery] string? gateway, [FromQuery] string? algorithm)
    {
        var report = await _topology.GetConnectivityAsync(gateway, algorithm);
        return report is null ? NotFound(new { error = "Unknown gateway." }) : Ok(report);
    }

    [HttpPost("nodes/{id}/down")]
    public IActionResult MarkDown(string id) =>
        _topology.SetDown(id, true) ? Ok(new { id, down = true }) : NotFound(new { error = $"No node matches '{id}'." });

    [HttpPost("nodes/{id}/up")]
    public IActionResult MarkUp(string id) =>
        _topology.SetDown(id, false) ? Ok(new { id, down = false }) : NotFound(new { error = $"No node matches '{id}'." });


    [HttpPost("seed-demo")]
    public async Task<ActionResult<SeedDemoResultDto>> SeedDemo() => Ok(await _topology.SeedDemoAsync());
}
