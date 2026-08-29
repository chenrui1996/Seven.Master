using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seven.Application.Pda;
using Seven.Domain.Common;
using Seven.Domain.Wms;

namespace Seven.WebApi.Controllers.Pda;

/// <summary>PDA 薄面：扫码收货/上架/盘点。权限码 Pda.* 可在菜单种子后收紧。</summary>
[Route("api/pda")]
[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = "pda")]
public class PdaController : ControllerBase
{
    private readonly IPdaService _pda;

    public PdaController(IPdaService pda) => _pda = pda;

    [HttpGet("menu")]
    public WebResponseContent Menu() => WebResponseContent.Ok(data: _pda.GetMenu());

    [HttpGet("inbound/pending")]
    public async Task<WebResponseContent> PendingInbound(CancellationToken ct) =>
        WebResponseContent.Ok(data: await _pda.GetPendingInboundAsync(ct));

    [HttpPost("inbound/{id:int}/receive")]
    public async Task<WebResponseContent> Receive(int id, [FromBody] PdaReceiveRequest request, CancellationToken ct)
    {
        try
        {
            return WebResponseContent.Ok("收货成功", await _pda.ReceiveFloorAsync(id, request, ct));
        }
        catch (WmsDomainException ex)
        {
            return WebResponseContent.Error(ex.Message);
        }
    }

    [HttpGet("putaway/pending")]
    public async Task<WebResponseContent> PendingPutaway(CancellationToken ct) =>
        WebResponseContent.Ok(data: await _pda.GetPendingPutawayAsync(ct));

    [HttpPost("putaway/confirm")]
    public async Task<WebResponseContent> ConfirmPutaway([FromBody] PdaPutawayConfirmRequest request, CancellationToken ct)
    {
        try
        {
            return WebResponseContent.Ok("上架成功", await _pda.ConfirmPutawayAsync(request, ct));
        }
        catch (WmsDomainException ex)
        {
            return WebResponseContent.Error(ex.Message);
        }
    }

    [HttpGet("cyclecount/pending")]
    public async Task<WebResponseContent> PendingCycleCount(CancellationToken ct) =>
        WebResponseContent.Ok(data: await _pda.GetPendingCycleCountsAsync(ct));

    [HttpGet("cyclecount/{id:int}")]
    public async Task<WebResponseContent> GetCycleCount(int id, CancellationToken ct)
    {
        try
        {
            return WebResponseContent.Ok(data: await _pda.GetCycleCountAsync(id, ct));
        }
        catch (WmsDomainException ex)
        {
            return WebResponseContent.Error(ex.Message);
        }
    }

    [HttpPost("cyclecount/{id:int}/record")]
    public async Task<WebResponseContent> RecordCycleCount(
        int id,
        [FromBody] PdaCycleCountRecordRequest request,
        CancellationToken ct)
    {
        try
        {
            return WebResponseContent.Ok("实盘录入成功", await _pda.RecordCycleCountAsync(id, request, ct));
        }
        catch (WmsDomainException ex)
        {
            return WebResponseContent.Error(ex.Message);
        }
    }

    [HttpPost("cyclecount/{id:int}/confirm")]
    public async Task<WebResponseContent> ConfirmCycleCount(int id, CancellationToken ct)
    {
        try
        {
            await _pda.ConfirmCycleCountAsync(id, ct);
            return WebResponseContent.Ok("盘点调账成功");
        }
        catch (WmsDomainException ex)
        {
            return WebResponseContent.Error(ex.Message);
        }
    }
}
