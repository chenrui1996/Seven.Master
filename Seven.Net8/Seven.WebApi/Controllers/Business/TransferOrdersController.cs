using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Seven.Application.Business;
using Seven.Domain.Business;
using Seven.Domain.Common;
using Seven.Domain.Entities.Business;
using Seven.Infrastructure.Crud;
using Seven.Infrastructure.Security;
using Seven.WebApi.Controllers.Crud;

namespace Seven.WebApi.Controllers.Business;

file static class BizEntityCrud
{
    public static async Task<WebResponseContent> Page<T>(EntityCrudService<T> crud, PageDataOptions options, CancellationToken ct)
        where T : class, new() =>
        WebResponseContent.Ok(data: await crud.GetPageDataAsync(options, ct));

    public static Task<WebResponseContent> Add<T>(EntityCrudService<T> crud, T entity, CancellationToken ct)
        where T : class, new() =>
        crud.AddAsync(entity, ct);

    public static Task<WebResponseContent> Update<T>(EntityCrudService<T> crud, T entity, CancellationToken ct)
        where T : class, new() =>
        crud.UpdateAsync(entity, ct);

    public static Task<WebResponseContent> Del<T>(EntityCrudService<T> crud, JsonElement ids, CancellationToken ct)
        where T : class, new() =>
        crud.DeleteAsync(ids, ct);
}

/// <summary>仓内调拨（业务扩展样板）。权限前缀 TransferOrder.* 与菜单 TableName 一致。</summary>
[Route("api/TransferOrder")]
[ApiController]
[Authorize]
public class TransferOrdersController : ControllerBase
{
    private readonly ITransferOrderService _orders;

    public TransferOrdersController(ITransferOrderService orders) => _orders = orders;

    [HttpPost("getPageData")]
    [Permission("TransferOrder.Search")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _orders.GetPageDataAsync(options, ct));

    [HttpGet("{id:int}")]
    [Permission("TransferOrder.Search")]
    public async Task<WebResponseContent> Get(int id, CancellationToken ct)
    {
        var order = await _orders.GetAsync(id, ct);
        return order == null ? WebResponseContent.Error("调拨单不存在") : WebResponseContent.Ok(data: order);
    }

    [HttpPost("add")]
    [Permission("TransferOrder.Add")]
    public async Task<WebResponseContent> Create([FromBody] CreateTransferOrderRequest request, CancellationToken ct)
    {
        try
        {
            return WebResponseContent.Ok("操作成功", await _orders.CreateAsync(request, ct));
        }
        catch (BizDomainException ex)
        {
            return WebResponseContent.Error(ex.Message, ex.Code);
        }
    }

    [HttpPost("approve/{id:int}")]
    [Permission("TransferOrder.Update")]
    public async Task<WebResponseContent> Approve(int id, CancellationToken ct)
    {
        try
        {
            await _orders.ApproveAsync(id, ct);
            return WebResponseContent.Ok("操作成功");
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

    [HttpPost("complete/{id:int}")]
    [Permission("TransferOrder.Update")]
    public async Task<WebResponseContent> Complete(int id, CancellationToken ct)
    {
        try
        {
            await _orders.CompleteAsync(id, ct);
            return WebResponseContent.Ok("操作成功");
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

    [HttpPost("export")]
    [Permission("TransferOrder.Export")]
    public Task<IActionResult> Export(
        [FromBody] PageDataOptions options,
        [FromServices] EntityCrudService<TransferOrder> crud,
        CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

    [HttpGet("exportTemplate")]
    [Permission("TransferOrder.Import")]
    public IActionResult ExportTemplate([FromServices] EntityCrudService<TransferOrder> crud) =>
        EntityCrudExcel.ExportTemplate(this, crud);

    [HttpPost("import")]
    [Permission("TransferOrder.Import")]
    [RequestSizeLimit(50_000_000)]
    public Task<WebResponseContent> Import(
        IFormFile file,
        [FromServices] EntityCrudService<TransferOrder> crud,
        CancellationToken ct) =>
        EntityCrudExcel.ImportAsync(crud, file, ct);
}

/// <summary>调拨行通用 CRUD，供 CrudPanel 明细表使用。</summary>
[Route("api/TransferOrderLine")]
[ApiController]
[Authorize]
public class TransferOrderLineController(EntityCrudService<TransferOrderLine> crud) : ControllerBase
{
    [HttpPost("getPageData")]
    [Permission("TransferOrder.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        BizEntityCrud.Page(crud, options, ct);

    [HttpPost("add")]
    [Permission("TransferOrder.Add")]
    public Task<WebResponseContent> Add([FromBody] TransferOrderLine entity, CancellationToken ct) =>
        BizEntityCrud.Add(crud, entity, ct);

    [HttpPost("update")]
    [Permission("TransferOrder.Update")]
    public Task<WebResponseContent> Update([FromBody] TransferOrderLine entity, CancellationToken ct) =>
        BizEntityCrud.Update(crud, entity, ct);

    [HttpPost("del")]
    [Permission("TransferOrder.Delete")]
    public Task<WebResponseContent> Del([FromBody] JsonElement ids, CancellationToken ct) =>
        BizEntityCrud.Del(crud, ids, ct);

    [HttpPost("export")]
    [Permission("TransferOrder.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

    [HttpGet("exportTemplate")]
    [Permission("TransferOrder.Import")]
    public IActionResult ExportTemplate() =>
        EntityCrudExcel.ExportTemplate(this, crud);

    [HttpPost("import")]
    [Permission("TransferOrder.Import")]
    [RequestSizeLimit(50_000_000)]
    public Task<WebResponseContent> Import(IFormFile file, CancellationToken ct) =>
        EntityCrudExcel.ImportAsync(crud, file, ct);
}
