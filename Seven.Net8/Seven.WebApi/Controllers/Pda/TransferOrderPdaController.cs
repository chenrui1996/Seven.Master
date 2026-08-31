using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Business;
using Seven.Domain.Business;
using Seven.Domain.Common;
using Seven.Domain.Enums;
using Seven.Infrastructure.Persistence;

namespace Seven.WebApi.Controllers.Pda;

/// <summary>业务扩展 PDA：仓内调拨（doc/23 样板）。</summary>
[Route("api/pda/biz/transfer")]
[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = "pda")]
public class TransferOrderPdaController : ControllerBase
{
    private readonly SevenDbContext _db;
    private readonly ITransferOrderService _orders;

    public TransferOrderPdaController(SevenDbContext db, ITransferOrderService orders)
    {
        _db = db;
        _orders = orders;
    }

    [HttpGet("pending")]
    public async Task<WebResponseContent> Pending(CancellationToken ct)
    {
        var rows = await _db.TransferOrders.AsNoTracking()
            .Include(x => x.Lines)
            .Where(x => x.Status == WmsOrderStatus.Approved)
            .OrderByDescending(x => x.Id)
            .Take(50)
            .Select(x => new
            {
                x.Id,
                x.OrderNo,
                x.Status,
                x.Remark,
                Lines = x.Lines.OrderBy(l => l.LineNo).Select(l => new
                {
                    l.LineNo,
                    l.MaterialCode,
                    l.Qty,
                    l.CompletedQty,
                    l.FromLocation,
                    l.ToLocation,
                    l.ContainerCode
                })
            })
            .ToListAsync(ct);
        return WebResponseContent.Ok(data: rows);
    }

    [HttpPost("complete/{id:int}")]
    public async Task<WebResponseContent> Complete(int id, CancellationToken ct)
    {
        try
        {
            await _orders.CompleteAsync(id, ct);
            return WebResponseContent.Ok("调拨完成");
        }
        catch (BizDomainException ex)
        {
            return WebResponseContent.Error(ex.Message, ex.Code);
        }
        catch (AppException ex)
        {
            return WebResponseContent.Error(ex.Message, ex.Code);
        }
    }
}
