using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Seven.Application.Interfaces;
using Seven.Domain.Common;
using Seven.Domain.Entities.DeviceComm;
using Seven.Infrastructure.Configuration;
using Seven.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace Seven.WebApi.Controllers;

/// <summary>设备通讯运行态 API</summary>
[Route("api/DeviceComm")]
[ApiController]
[Authorize]
[RequiresFeature("DeviceComm")]
[ApiExplorerSettings(GroupName = "ops")]
public class DeviceCommController : ControllerBase
{
    private readonly IDeviceCommGateway _gateway;
    private readonly ICommRuleEngine _rules;
    private readonly IOptions<FeatureOptions> _features;

    public DeviceCommController(
        IDeviceCommGateway gateway,
        ICommRuleEngine rules,
        IOptions<FeatureOptions> features)
    {
        _gateway = gateway;
        _rules = rules;
        _features = features;
    }

    [HttpGet("status")]
    [Permission("DeviceComm.Search")]
    public async Task<WebResponseContent> Status(CancellationToken ct)
    {
        if (!_features.Value.DeviceComm)
            return WebResponseContent.Error("DeviceComm 未启用");
        return WebResponseContent.Ok(data: await _gateway.GetStatusesAsync(ct));
    }

    [HttpPost("connect/{id:int}")]
    [Permission("DeviceComm.Update")]
    public async Task<WebResponseContent> Connect(int id, CancellationToken ct)
    {
        await _gateway.ConnectAsync(id, ct);
        return WebResponseContent.Ok("已连接", await _gateway.GetStatusAsync(id, ct));
    }

    [HttpPost("disconnect/{id:int}")]
    [Permission("DeviceComm.Update")]
    public async Task<WebResponseContent> Disconnect(int id, CancellationToken ct)
    {
        await _gateway.DisconnectAsync(id, ct);
        return WebResponseContent.Ok("已断开");
    }

    [HttpPost("reconnect/{id:int}")]
    [Permission("DeviceComm.Update")]
    public async Task<WebResponseContent> Reconnect(int id, CancellationToken ct)
    {
        await _gateway.ReconnectAsync(id, ct);
        return WebResponseContent.Ok("已重连", await _gateway.GetStatusAsync(id, ct));
    }

    [HttpPost("read")]
    [Permission("DeviceComm.Search")]
    public async Task<WebResponseContent> Read([FromBody] int[] pointIds, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _gateway.ReadPointsAsync(pointIds ?? [], ct));

    [HttpPost("write")]
    [Permission("DeviceComm.Update")]
    public async Task<WebResponseContent> Write([FromBody] List<CommWritePointRequest> writes, CancellationToken ct)
    {
        await _gateway.WritePointsAsync(writes ?? [], ct);
        return WebResponseContent.Ok("写入成功");
    }

    [HttpPost("readRaw")]
    [Permission("DeviceComm.Search")]
    public async Task<WebResponseContent> ReadRaw([FromBody] CommRawIoRequest request, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _gateway.ReadRawAsync(request, ct));

    [HttpPost("writeRaw")]
    [Permission("DeviceComm.Update")]
    public async Task<WebResponseContent> WriteRaw([FromBody] CommRawIoRequest request, CancellationToken ct)
    {
        await _gateway.WriteRawAsync(request, ct);
        return WebResponseContent.Ok("写入成功");
    }

    [HttpPost("reload")]
    [Permission("DeviceComm.Update")]
    public async Task<WebResponseContent> Reload(CancellationToken ct)
    {
        await _gateway.ReloadAsync(ct);
        await _rules.ReloadAsync(ct);
        return WebResponseContent.Ok("已重载连接与规则");
    }

    [HttpPost("triggerRule/{id:int}")]
    [Permission("DeviceComm.Update")]
    public async Task<WebResponseContent> TriggerRule(int id, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _rules.TriggerAsync(id, ct));
}

[Route("api/CommConnection")]
[ApiController]
[Authorize]
[RequiresFeature("DeviceComm")]
public class CommConnectionController : ControllerBase
{
    private readonly ICommConnectionService _service;
    public CommConnectionController(ICommConnectionService service) => _service = service;

    [HttpPost("getPageData")]
    [Permission("CommConnection.Search")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _service.GetPageDataAsync(options, ct));

    [HttpPost("add")]
    [Permission("CommConnection.Add")]
    public Task<WebResponseContent> Add([FromBody] CommConnection entity, CancellationToken ct) =>
        _service.AddAsync(entity, ct);

    [HttpPost("update")]
    [Permission("CommConnection.Update")]
    public Task<WebResponseContent> Update([FromBody] CommConnection entity, CancellationToken ct) =>
        _service.UpdateAsync(entity, ct);

    [HttpPost("del")]
    [Permission("CommConnection.Delete")]
    public Task<WebResponseContent> Delete([FromBody] int[] ids, CancellationToken ct) =>
        _service.DeleteAsync(ids, ct);
}

[Route("api/CommPoint")]
[ApiController]
[Authorize]
[RequiresFeature("DeviceComm")]
public class CommPointController : ControllerBase
{
    private readonly ICommPointService _service;
    public CommPointController(ICommPointService service) => _service = service;

    [HttpPost("getPageData")]
    [Permission("CommPoint.Search")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _service.GetPageDataAsync(options, ct));

    [HttpPost("add")]
    [Permission("CommPoint.Add")]
    public Task<WebResponseContent> Add([FromBody] CommPoint entity, CancellationToken ct) =>
        _service.AddAsync(entity, ct);

    [HttpPost("update")]
    [Permission("CommPoint.Update")]
    public Task<WebResponseContent> Update([FromBody] CommPoint entity, CancellationToken ct) =>
        _service.UpdateAsync(entity, ct);

    [HttpPost("del")]
    [Permission("CommPoint.Delete")]
    public Task<WebResponseContent> Delete([FromBody] int[] ids, CancellationToken ct) =>
        _service.DeleteAsync(ids, ct);
}

[Route("api/CommRule")]
[ApiController]
[Authorize]
[RequiresFeature("DeviceComm")]
public class CommRuleController : ControllerBase
{
    private readonly ICommRuleService _service;
    public CommRuleController(ICommRuleService service) => _service = service;

    [HttpPost("getPageData")]
    [Permission("CommRule.Search")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _service.GetPageDataAsync(options, ct));

    [HttpPost("add")]
    [Permission("CommRule.Add")]
    public Task<WebResponseContent> Add([FromBody] CommRule entity, CancellationToken ct) =>
        _service.AddAsync(entity, ct);

    [HttpPost("update")]
    [Permission("CommRule.Update")]
    public Task<WebResponseContent> Update([FromBody] CommRule entity, CancellationToken ct) =>
        _service.UpdateAsync(entity, ct);

    [HttpPost("del")]
    [Permission("CommRule.Delete")]
    public Task<WebResponseContent> Delete([FromBody] int[] ids, CancellationToken ct) =>
        _service.DeleteAsync(ids, ct);

    [HttpPost("eventLogs")]
    [Permission("CommRule.Search")]
    public async Task<WebResponseContent> EventLogs([FromBody] PageDataOptions options, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _service.GetEventLogsAsync(options, ct));
}
