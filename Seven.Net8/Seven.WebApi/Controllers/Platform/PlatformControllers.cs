using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seven.Application.Platform;
using Seven.Domain.Common;
using Seven.Domain.Enums;
using Seven.Infrastructure.Security;

namespace Seven.WebApi.Controllers.Platform;

[Route("api/ControlMode")]
[ApiController]
[Authorize]
public class ControlModeController : ControllerBase
{
    private readonly IControlModeService _controlMode;

    public ControlModeController(IControlModeService controlMode) => _controlMode = controlMode;

    [HttpGet("{scope}")]
    public async Task<WebResponseContent> Get(string scope, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _controlMode.GetAsync(scope, ct));

    [HttpPost("{scope}/mode")]
    public async Task<WebResponseContent> SetMode(string scope, [FromBody] SetControlModeRequest request, CancellationToken ct)
    {
        await _controlMode.SetModeAsync(scope, request.Mode, ct);
        return WebResponseContent.Ok("模式已更新", await _controlMode.GetAsync(scope, ct));
    }

    [HttpPost("{scope}/estop")]
    public async Task<WebResponseContent> SetEStop(string scope, [FromBody] SetEStopRequest request, CancellationToken ct)
    {
        await _controlMode.SetEStopAsync(scope, request.EStop, ct);
        return WebResponseContent.Ok("急停状态已更新", await _controlMode.GetAsync(scope, ct));
    }
}

[Route("api/InterfaceLog")]
[ApiController]
[Authorize]
public class InterfaceLogController : ControllerBase
{
    private readonly IInterfaceLogService _logs;

    public InterfaceLogController(IInterfaceLogService logs) => _logs = logs;

    [HttpGet("count")]
    public async Task<WebResponseContent> Count(CancellationToken ct) =>
        WebResponseContent.Ok(data: await _logs.CountAsync(ct));

    [HttpPost("getPageData")]
    [Permission("IfcApiLog.Search")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _logs.GetPageDataAsync(options, ct));
}

public record SetControlModeRequest(WcsControlMode Mode);

public record SetEStopRequest(bool EStop);
