using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seven.Application.Scada;
using Seven.Domain.Common;
using Seven.Domain.Entities.Platform;
using Seven.Infrastructure.Security;

namespace Seven.WebApi.Controllers.Scada;

[Route("api/ScdView")]
[ApiController]
[Authorize]
public class ScdViewsController : ControllerBase
{
    private readonly IScadaViewService _views;

    public ScdViewsController(IScadaViewService views) => _views = views;

    [HttpGet("{code}")]
    [Permission("ScdView.Search")]
    public async Task<WebResponseContent> GetByCode(string code, CancellationToken ct)
    {
        var view = await _views.GetByCodeAsync(code, ct);
        return view == null ? WebResponseContent.Error("è§å¾ä¸å­å¨") : WebResponseContent.Ok(data: view);
    }

    [HttpGet("{id:int}/status")]
    [Permission("ScdView.Search")]
    public async Task<WebResponseContent> GetStatus(int id, CancellationToken ct)
    {
        var status = await _views.GetStatusAsync(id, ct);
        return status == null ? WebResponseContent.Error("è§å¾ä¸å­å¨") : WebResponseContent.Ok(data: status);
    }

    [HttpPost("getPageData")]
    [Permission("ScdView.Search")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _views.GetPageDataAsync(options, ct));

    [HttpPost("add")]
    [Permission("ScdView.Add")]
    public Task<WebResponseContent> Add([FromBody] ScdView entity, CancellationToken ct) =>
        _views.AddAsync(entity, ct);

    [HttpPost("update")]
    [Permission("ScdView.Update")]
    public Task<WebResponseContent> Update([FromBody] ScdView entity, CancellationToken ct) =>
        _views.UpdateAsync(entity, ct);
}

[Route("api/ScdNodeBind")]
[ApiController]
[Authorize]
public class ScdNodeBindsController : ControllerBase
{
    private readonly IScadaNodeBindService _binds;

    public ScdNodeBindsController(IScadaNodeBindService binds) => _binds = binds;

    [HttpGet("byView/{viewId:int}")]
    [Permission("ScdNodeBind.Search")]
    public async Task<WebResponseContent> ListByView(int viewId, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _binds.ListByViewIdAsync(viewId, ct));

    [HttpPost("getPageData")]
    [Permission("ScdNodeBind.Search")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _binds.GetPageDataAsync(options, ct));

    [HttpPost("add")]
    [Permission("ScdNodeBind.Add")]
    public Task<WebResponseContent> Add([FromBody] ScdNodeBind entity, CancellationToken ct) =>
        _binds.AddAsync(entity, ct);

    [HttpPost("update")]
    [Permission("ScdNodeBind.Update")]
    public Task<WebResponseContent> Update([FromBody] ScdNodeBind entity, CancellationToken ct) =>
        _binds.UpdateAsync(entity, ct);
}
