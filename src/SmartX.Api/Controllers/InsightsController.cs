using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;
using SmartX.Shared.Dtos;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/insights")]
public sealed class InsightsController : ControllerBase
{
    private readonly PredictiveInsightService _insights;

    public InsightsController(PredictiveInsightService insights)
    {
        _insights = insights;
    }

    [HttpGet("suggestions")]
    public async Task<ActionResult<IReadOnlyList<SuggestedActionDto>>> Suggestions() =>
        Ok(await _insights.GetSuggestionsAsync());
}
