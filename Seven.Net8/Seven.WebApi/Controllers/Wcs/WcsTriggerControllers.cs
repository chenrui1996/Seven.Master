using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Seven.Application.Wcs;
using Seven.Domain.Common;
using Seven.Infrastructure.Configuration;

namespace Seven.WebApi.Controllers.Wcs;

[Route("api/Wcs/Triggers")]
[ApiController]
[Authorize]
public class WcsTriggersController : ControllerBase
{
    private readonly IEquipmentTriggerPort _port;
    private readonly FeatureOptions _features;

    public WcsTriggersController(IEquipmentTriggerPort port, IOptions<FeatureOptions> features)
    {
        _port = port;
        _features = features.Value;
    }

    [HttpPost("destination-request")]
    public async Task<IActionResult> DestinationRequest(
        [FromBody] DestinationRequestTrigger trigger,
        CancellationToken ct)
    {
        if (!IsSimulationEnabled())
            return FeatureOff();
        await _port.SimulateDestinationRequestAsync(trigger, ct);
        return Ok(WebResponseContent.Ok("已仿真目的地申请"));
    }

    [HttpPost("segment-feedback")]
    public async Task<IActionResult> SegmentFeedback(
        [FromBody] DeviceSegmentFeedback feedback,
        CancellationToken ct)
    {
        if (!IsSimulationEnabled())
            return FeatureOff();
        await _port.SimulateSegmentFeedbackAsync(feedback, ct);
        return Ok(WebResponseContent.Ok("已仿真段反馈"));
    }

    private bool IsSimulationEnabled() =>
        _features.IsWcsPackEnabled("Stacker")
        || _features.IsWcsPackEnabled("FourWay")
        || _features.OrchestrationBus;

    private static ObjectResult FeatureOff() =>
        new(WebResponseContent.Error("功能未启用: Wcs simulation"))
        {
            StatusCode = StatusCodes.Status404NotFound
        };
}
