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

    /// <summary>
    /// The Automated Action Engine's current suggestions — one per active
    /// alert, naming the historically most-likely operator response, plus
    /// any troubled sensor that doesn't have a learned pattern yet.
    /// </summary>
    [HttpGet("suggestions")]
    public async Task<ActionResult<IReadOnlyList<SuggestedActionDto>>> Suggestions() =>
        Ok(await _insights.GetSuggestionsAsync());
}
