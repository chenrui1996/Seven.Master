using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seven.Application.Wcs;
using Seven.Domain.Common;

namespace Seven.WebApi.Controllers.Wcs;

[Route("api/Wcs/Triggers")]
[ApiController]
[Authorize]
public class WcsTriggersController : ControllerBase
{
    private readonly IEquipmentTriggerPort _port;

    public WcsTriggersController(IEquipmentTriggerPort port) => _port = port;

    [HttpPost("destination-request")]
    public async Task<IActionResult> DestinationRequest(
        [FromBody] DestinationRequestTrigger trigger,
        CancellationToken ct)
    {
        await _port.SimulateDestinationRequestAsync(trigger, ct);
        return Ok(WebResponseContent.Ok("已仿真目的地申请"));
    }

    [HttpPost("segment-feedback")]
    public async Task<IActionResult> SegmentFeedback(
        [FromBody] DeviceSegmentFeedback feedback,
        CancellationToken ct)
    {
        await _port.SimulateSegmentFeedbackAsync(feedback, ct);
        return Ok(WebResponseContent.Ok("已仿真段反馈"));
    }
}
