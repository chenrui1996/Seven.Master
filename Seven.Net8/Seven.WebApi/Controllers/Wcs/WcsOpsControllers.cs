using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seven.Application.Platform;
using Seven.Application.Wcs;
using Seven.Domain.Common;
using Seven.Domain.Enums;
using Seven.Infrastructure.Security;

namespace Seven.WebApi.Controllers.Wcs;

[Route("api/Wcs/FourWay/Ops")]
[ApiController]
[Authorize]
public class FourWayOpsController : ControllerBase
{
    private readonly IFourWayOpsService _ops;
    private readonly IControlModeService _control;

    public FourWayOpsController(IFourWayOpsService ops, IControlModeService control)
    {
        _ops = ops;
        _control = control;
    }

    [HttpGet("meta")]
    [Permission("FwOpsInbound.Search")]
    public async Task<WebResponseContent> Meta(CancellationToken ct) =>
        WebResponseContent.Ok(data: await _ops.GetMetaAsync(ct));

    [HttpGet("board")]
    [Permission("FwOpsMonitor.Search")]
    public async Task<WebResponseContent> Board(CancellationToken ct) =>
        WebResponseContent.Ok(data: await _ops.GetBoardAsync(ct));

    [HttpGet("task-tree")]
    [Permission("FwOpsShuttle.Search")]
    public async Task<WebResponseContent> TaskTree(
        [FromQuery] Guid? shuttleTaskId,
        [FromQuery] Guid? putAwayId,
        [FromQuery] Guid? retrievalId,
        [FromQuery] Guid? hoistTaskId,
        CancellationToken ct)
    {
        var tree = await _ops.GetTaskTreeAsync(shuttleTaskId, putAwayId, retrievalId, hoistTaskId, ct);
        return tree == null ? WebResponseContent.Error("未找到任务树") : WebResponseContent.Ok(data: tree);
    }

    [HttpPost("inbound")]
    [Permission("FwOpsInbound.Add")]
    public async Task<WebResponseContent> Inbound([FromBody] FourWayOpsInboundRequest req, CancellationToken ct)
    {
        var (ok, msg, data) = await _ops.CreateInboundAsync(req, ct);
        return ok ? WebResponseContent.Ok(msg, data) : WebResponseContent.Error(msg);
    }

    [HttpGet("inbound/pickable-map")]
    [Permission("FwOpsInbound.Search")]
    public async Task<WebResponseContent> PickableMap([FromQuery] string? layerCode, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _ops.GetPickableMapAsync(layerCode, ct));

    [HttpPost("point-dispatch")]
    [Permission("FwOpsShuttle.Update")]
    public async Task<WebResponseContent> PointDispatch([FromBody] FourWayOpsPointDispatchRequest req, CancellationToken ct)
    {
        var (ok, msg, data) = await _ops.PointDispatchAsync(req, ct);
        return ok ? WebResponseContent.Ok(msg, data) : WebResponseContent.Error(msg);
    }

    [HttpPost("charge")]
    [Permission("FwOpsShuttle.Update")]
    public async Task<WebResponseContent> Charge([FromBody] FourWayOpsChargeRequest req, CancellationToken ct)
    {
        var (ok, msg, data) = await _ops.ChargeAsync(req, stop: false, ct);
        return ok ? WebResponseContent.Ok(msg, data) : WebResponseContent.Error(msg);
    }

    [HttpPost("charge/stop")]
    [Permission("FwOpsShuttle.Update")]
    public async Task<WebResponseContent> ChargeStop([FromBody] FourWayOpsChargeRequest? req, CancellationToken ct)
    {
        var (ok, msg, data) = await _ops.ChargeAsync(req ?? new FourWayOpsChargeRequest("", "", null), stop: true, ct);
        return ok ? WebResponseContent.Ok(msg, data) : WebResponseContent.Error(msg);
    }

    [HttpPost("force-complete")]
    [Permission("FwOpsShuttle.Update")]
    public async Task<WebResponseContent> ForceComplete([FromBody] FourWayOpsForceCompleteRequest req, CancellationToken ct)
    {
        var (ok, msg) = await _ops.ForceCompleteAsync(req, ct);
        return ok ? WebResponseContent.Ok(msg) : WebResponseContent.Error(msg);
    }

    [HttpPost("resend")]
    [Permission("FwOpsShuttle.Update")]
    public async Task<WebResponseContent> Resend([FromBody] FourWayOpsResendRequest req, CancellationToken ct)
    {
        var (ok, msg) = await _ops.ResendAsync(req, ct);
        return ok ? WebResponseContent.Ok(msg) : WebResponseContent.Error(msg);
    }

    [HttpGet("control-mode")]
    [Permission("FwOpsCtlMode.Search")]
    public async Task<WebResponseContent> GetControlMode(CancellationToken ct) =>
        WebResponseContent.Ok(data: await _control.GetAsync("FourWay", ct));

    [HttpPost("control-mode")]
    [Permission("FwOpsCtlMode.Update")]
    public async Task<WebResponseContent> PutControlMode([FromBody] OpsControlModeBody body, CancellationToken ct)
    {
        if (body.Mode is WcsControlMode mode)
            await _control.SetModeAsync("FourWay", mode, ct);
        if (body.EStop is bool eStop)
            await _control.SetEStopAsync("FourWay", eStop, ct);
        if (body.GlobalEStop is bool g)
            await _control.SetEStopAsync("Global", g, ct);
        return WebResponseContent.Ok("已更新", await _control.GetAsync("FourWay", ct));
    }
}

[Route("api/Wcs/Stacker/Ops")]
[ApiController]
[Authorize]
public class StackerOpsController : ControllerBase
{
    private readonly IStackerOpsService _ops;
    private readonly IControlModeService _control;

    public StackerOpsController(IStackerOpsService ops, IControlModeService control)
    {
        _ops = ops;
        _control = control;
    }

    [HttpGet("board")]
    [Permission("StkOpsMonitor.Search")]
    public async Task<WebResponseContent> Board(CancellationToken ct) =>
        WebResponseContent.Ok(data: await _ops.GetBoardAsync(ct));

    [HttpGet("task-tree")]
    [Permission("StkOpsSrm.Search")]
    public async Task<WebResponseContent> TaskTree(
        [FromQuery] Guid? putAwayId,
        [FromQuery] Guid? retrievalId,
        [FromQuery] Guid? deviceTaskId,
        CancellationToken ct)
    {
        var tree = await _ops.GetTaskTreeAsync(putAwayId, retrievalId, deviceTaskId, ct);
        return tree == null ? WebResponseContent.Error("未找到任务树") : WebResponseContent.Ok(data: tree);
    }

    [HttpPost("force-complete")]
    [Permission("StkOpsSrm.Update")]
    public async Task<WebResponseContent> ForceComplete([FromBody] StackerOpsForceCompleteRequest req, CancellationToken ct)
    {
        var (ok, msg) = await _ops.ForceCompleteAsync(req, ct);
        return ok ? WebResponseContent.Ok(msg) : WebResponseContent.Error(msg);
    }

    [HttpPost("resend")]
    [Permission("StkOpsSrm.Update")]
    public async Task<WebResponseContent> Resend([FromBody] StackerOpsResendRequest req, CancellationToken ct)
    {
        var (ok, msg) = await _ops.ResendAsync(req, ct);
        return ok ? WebResponseContent.Ok(msg) : WebResponseContent.Error(msg);
    }

    [HttpGet("request-points")]
    [Permission("StkOpsRequest.Search")]
    public async Task<WebResponseContent> RequestPoints(CancellationToken ct) =>
        WebResponseContent.Ok(data: await _ops.ListRequestPointsAsync(ct));

    [HttpPost("request-point/{id:int}/enable")]
    [Permission("StkOpsRequest.Update")]
    public async Task<WebResponseContent> EnableRequestPoint(int id, CancellationToken ct)
    {
        var (ok, msg) = await _ops.SetRequestPointEnabledAsync(id, true, ct);
        return ok ? WebResponseContent.Ok(msg) : WebResponseContent.Error(msg);
    }

    [HttpPost("request-point/{id:int}/disable")]
    [Permission("StkOpsRequest.Update")]
    public async Task<WebResponseContent> DisableRequestPoint(int id, CancellationToken ct)
    {
        var (ok, msg) = await _ops.SetRequestPointEnabledAsync(id, false, ct);
        return ok ? WebResponseContent.Ok(msg) : WebResponseContent.Error(msg);
    }

    [HttpGet("control-mode")]
    [Permission("StkOpsCtlMode.Search")]
    public async Task<WebResponseContent> GetControlMode(CancellationToken ct) =>
        WebResponseContent.Ok(data: await _control.GetAsync("Stacker", ct));

    [HttpPost("control-mode")]
    [Permission("StkOpsCtlMode.Update")]
    public async Task<WebResponseContent> PutControlMode([FromBody] OpsControlModeBody body, CancellationToken ct)
    {
        if (body.Mode is WcsControlMode mode)
            await _control.SetModeAsync("Stacker", mode, ct);
        if (body.EStop is bool eStop)
            await _control.SetEStopAsync("Stacker", eStop, ct);
        if (body.GlobalEStop is bool g)
            await _control.SetEStopAsync("Global", g, ct);
        return WebResponseContent.Ok("已更新", await _control.GetAsync("Stacker", ct));
    }
}

public record OpsControlModeBody(WcsControlMode? Mode, bool? EStop, bool? GlobalEStop);
