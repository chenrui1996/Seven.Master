using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seven.Domain.Common;
using Seven.Domain.Entities.Wms;
using Seven.Infrastructure.Crud;
using Seven.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Seven.WebApi.Controllers.Crud;

namespace Seven.WebApi.Controllers.Wms;

file static class EntityCrudController
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

[Route("api/WmsWarehouse")]
[ApiController]
[Authorize]
public class WmsWarehouseController(EntityCrudService<WmsWarehouse> crud) : ControllerBase
{
    [HttpPost("getPageData")]
    [Permission("WmsWarehouse.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudController.Page(crud, options, ct);

    [HttpPost("add")]
    [Permission("WmsWarehouse.Add")]
    public Task<WebResponseContent> Add([FromBody] WmsWarehouse entity, CancellationToken ct) =>
        EntityCrudController.Add(crud, entity, ct);

    [HttpPost("update")]
    [Permission("WmsWarehouse.Update")]
    public Task<WebResponseContent> Update([FromBody] WmsWarehouse entity, CancellationToken ct) =>
        EntityCrudController.Update(crud, entity, ct);

    [HttpPost("del")]
    [Permission("WmsWarehouse.Delete")]
    public Task<WebResponseContent> Del([FromBody] JsonElement ids, CancellationToken ct) =>
        EntityCrudController.Del(crud, ids, ct);

    [HttpPost("export")]
    [Permission("WmsWarehouse.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

    [HttpGet("exportTemplate")]
    [Permission("WmsWarehouse.Import")]
    public IActionResult ExportTemplate() =>
        EntityCrudExcel.ExportTemplate(this, crud);

    [HttpPost("import")]
    [Permission("WmsWarehouse.Import")]
    [RequestSizeLimit(50_000_000)]
    public Task<WebResponseContent> Import(IFormFile file, CancellationToken ct) =>
        EntityCrudExcel.ImportAsync(crud, file, ct);

}

[Route("api/WmsZone")]
[ApiController]
[Authorize]
public class WmsZoneController(EntityCrudService<WmsZone> crud) : ControllerBase
{
    [HttpPost("getPageData")]
    [Permission("WmsZone.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudController.Page(crud, options, ct);

    [HttpPost("add")]
    [Permission("WmsZone.Add")]
    public Task<WebResponseContent> Add([FromBody] WmsZone entity, CancellationToken ct) =>
        EntityCrudController.Add(crud, entity, ct);

    [HttpPost("update")]
    [Permission("WmsZone.Update")]
    public Task<WebResponseContent> Update([FromBody] WmsZone entity, CancellationToken ct) =>
        EntityCrudController.Update(crud, entity, ct);

    [HttpPost("del")]
    [Permission("WmsZone.Delete")]
    public Task<WebResponseContent> Del([FromBody] JsonElement ids, CancellationToken ct) =>
        EntityCrudController.Del(crud, ids, ct);

    [HttpPost("export")]
    [Permission("WmsZone.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

    [HttpGet("exportTemplate")]
    [Permission("WmsZone.Import")]
    public IActionResult ExportTemplate() =>
        EntityCrudExcel.ExportTemplate(this, crud);

    [HttpPost("import")]
    [Permission("WmsZone.Import")]
    [RequestSizeLimit(50_000_000)]
    public Task<WebResponseContent> Import(IFormFile file, CancellationToken ct) =>
        EntityCrudExcel.ImportAsync(crud, file, ct);

}

[Route("api/WmsLayer")]
[ApiController]
[Authorize]
public class WmsLayerController(EntityCrudService<WmsLayer> crud) : ControllerBase
{
    [HttpPost("getPageData")]
    [Permission("WmsLayer.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudController.Page(crud, options, ct);

    [HttpPost("add")]
    [Permission("WmsLayer.Add")]
    public Task<WebResponseContent> Add([FromBody] WmsLayer entity, CancellationToken ct) =>
        EntityCrudController.Add(crud, entity, ct);

    [HttpPost("update")]
    [Permission("WmsLayer.Update")]
    public Task<WebResponseContent> Update([FromBody] WmsLayer entity, CancellationToken ct) =>
        EntityCrudController.Update(crud, entity, ct);

    [HttpPost("del")]
    [Permission("WmsLayer.Delete")]
    public Task<WebResponseContent> Del([FromBody] JsonElement ids, CancellationToken ct) =>
        EntityCrudController.Del(crud, ids, ct);

    [HttpPost("export")]
    [Permission("WmsLayer.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

    [HttpGet("exportTemplate")]
    [Permission("WmsLayer.Import")]
    public IActionResult ExportTemplate() =>
        EntityCrudExcel.ExportTemplate(this, crud);

    [HttpPost("import")]
    [Permission("WmsLayer.Import")]
    [RequestSizeLimit(50_000_000)]
    public Task<WebResponseContent> Import(IFormFile file, CancellationToken ct) =>
        EntityCrudExcel.ImportAsync(crud, file, ct);

}

[Route("api/WmsAisle")]
[ApiController]
[Authorize]
public class WmsAisleController(EntityCrudService<WmsAisle> crud) : ControllerBase
{
    [HttpPost("getPageData")]
    [Permission("WmsAisle.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudController.Page(crud, options, ct);

    [HttpPost("add")]
    [Permission("WmsAisle.Add")]
    public Task<WebResponseContent> Add([FromBody] WmsAisle entity, CancellationToken ct) =>
        EntityCrudController.Add(crud, entity, ct);

    [HttpPost("update")]
    [Permission("WmsAisle.Update")]
    public Task<WebResponseContent> Update([FromBody] WmsAisle entity, CancellationToken ct) =>
        EntityCrudController.Update(crud, entity, ct);

    [HttpPost("del")]
    [Permission("WmsAisle.Delete")]
    public Task<WebResponseContent> Del([FromBody] JsonElement ids, CancellationToken ct) =>
        EntityCrudController.Del(crud, ids, ct);

    [HttpPost("export")]
    [Permission("WmsAisle.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

    [HttpGet("exportTemplate")]
    [Permission("WmsAisle.Import")]
    public IActionResult ExportTemplate() =>
        EntityCrudExcel.ExportTemplate(this, crud);

    [HttpPost("import")]
    [Permission("WmsAisle.Import")]
    [RequestSizeLimit(50_000_000)]
    public Task<WebResponseContent> Import(IFormFile file, CancellationToken ct) =>
        EntityCrudExcel.ImportAsync(crud, file, ct);

}

[Route("api/WmsContainerType")]
[ApiController]
[Authorize]
public class WmsContainerTypeController(EntityCrudService<WmsContainerType> crud) : ControllerBase
{
    [HttpPost("getPageData")]
    [Permission("WmsContainerType.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudController.Page(crud, options, ct);

    [HttpPost("add")]
    [Permission("WmsContainerType.Add")]
    public Task<WebResponseContent> Add([FromBody] WmsContainerType entity, CancellationToken ct) =>
        EntityCrudController.Add(crud, entity, ct);

    [HttpPost("update")]
    [Permission("WmsContainerType.Update")]
    public Task<WebResponseContent> Update([FromBody] WmsContainerType entity, CancellationToken ct) =>
        EntityCrudController.Update(crud, entity, ct);

    [HttpPost("del")]
    [Permission("WmsContainerType.Delete")]
    public Task<WebResponseContent> Del([FromBody] JsonElement ids, CancellationToken ct) =>
        EntityCrudController.Del(crud, ids, ct);

    [HttpPost("export")]
    [Permission("WmsContainerType.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

    [HttpGet("exportTemplate")]
    [Permission("WmsContainerType.Import")]
    public IActionResult ExportTemplate() =>
        EntityCrudExcel.ExportTemplate(this, crud);

    [HttpPost("import")]
    [Permission("WmsContainerType.Import")]
    [RequestSizeLimit(50_000_000)]
    public Task<WebResponseContent> Import(IFormFile file, CancellationToken ct) =>
        EntityCrudExcel.ImportAsync(crud, file, ct);

}

[Route("api/WmsHandoverLink")]
[ApiController]
[Authorize]
public class WmsHandoverLinkController(EntityCrudService<WmsHandoverLink> crud) : ControllerBase
{
    [HttpPost("getPageData")]
    [Permission("WmsHandoverLink.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudController.Page(crud, options, ct);

    [HttpPost("add")]
    [Permission("WmsHandoverLink.Add")]
    public Task<WebResponseContent> Add([FromBody] WmsHandoverLink entity, CancellationToken ct) =>
        EntityCrudController.Add(crud, entity, ct);

    [HttpPost("update")]
    [Permission("WmsHandoverLink.Update")]
    public Task<WebResponseContent> Update([FromBody] WmsHandoverLink entity, CancellationToken ct) =>
        EntityCrudController.Update(crud, entity, ct);

    [HttpPost("del")]
    [Permission("WmsHandoverLink.Delete")]
    public Task<WebResponseContent> Del([FromBody] JsonElement ids, CancellationToken ct) =>
        EntityCrudController.Del(crud, ids, ct);

    [HttpPost("export")]
    [Permission("WmsHandoverLink.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

    [HttpGet("exportTemplate")]
    [Permission("WmsHandoverLink.Import")]
    public IActionResult ExportTemplate() =>
        EntityCrudExcel.ExportTemplate(this, crud);

    [HttpPost("import")]
    [Permission("WmsHandoverLink.Import")]
    [RequestSizeLimit(50_000_000)]
    public Task<WebResponseContent> Import(IFormFile file, CancellationToken ct) =>
        EntityCrudExcel.ImportAsync(crud, file, ct);

}

[Route("api/WmsStockLedger")]
[ApiController]
[Authorize]
public class WmsStockLedgerController(EntityCrudService<WmsStockLedger> crud) : ControllerBase
{
    [HttpPost("getPageData")]
    [Permission("WmsStockLedger.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudController.Page(crud, options, ct);

    [HttpPost("export")]
    [Permission("WmsStockLedger.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

}

[Route("api/WmsInboundOrderLine")]
[ApiController]
[Authorize]
public class WmsInboundOrderLineController(EntityCrudService<WmsInboundOrderLine> crud) : ControllerBase
{
    [HttpPost("getPageData")]
    [Permission("WmsInboundOrder.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudController.Page(crud, options, ct);

    [HttpPost("add")]
    [Permission("WmsInboundOrder.Add")]
    public Task<WebResponseContent> Add([FromBody] WmsInboundOrderLine entity, CancellationToken ct) =>
        EntityCrudController.Add(crud, entity, ct);

    [HttpPost("update")]
    [Permission("WmsInboundOrder.Update")]
    public Task<WebResponseContent> Update([FromBody] WmsInboundOrderLine entity, CancellationToken ct) =>
        EntityCrudController.Update(crud, entity, ct);

    [HttpPost("del")]
    [Permission("WmsInboundOrder.Delete")]
    public Task<WebResponseContent> Del([FromBody] JsonElement ids, CancellationToken ct) =>
        EntityCrudController.Del(crud, ids, ct);

    [HttpPost("export")]
    [Permission("WmsInboundOrder.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

    [HttpGet("exportTemplate")]
    [Permission("WmsInboundOrder.Import")]
    public IActionResult ExportTemplate() =>
        EntityCrudExcel.ExportTemplate(this, crud);

    [HttpPost("import")]
    [Permission("WmsInboundOrder.Import")]
    [RequestSizeLimit(50_000_000)]
    public Task<WebResponseContent> Import(IFormFile file, CancellationToken ct) =>
        EntityCrudExcel.ImportAsync(crud, file, ct);

}

[Route("api/WmsInboundDetail")]
[ApiController]
[Authorize]
public class WmsInboundDetailController(EntityCrudService<WmsInboundDetail> crud) : ControllerBase
{
    [HttpPost("getPageData")]
    [Permission("WmsInboundOrder.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudController.Page(crud, options, ct);

    [HttpPost("add")]
    [Permission("WmsInboundOrder.Update")]
    public Task<WebResponseContent> Add([FromBody] WmsInboundDetail entity, CancellationToken ct) =>
        EntityCrudController.Add(crud, entity, ct);

    [HttpPost("update")]
    [Permission("WmsInboundOrder.Update")]
    public Task<WebResponseContent> Update([FromBody] WmsInboundDetail entity, CancellationToken ct) =>
        EntityCrudController.Update(crud, entity, ct);

    [HttpPost("del")]
    [Permission("WmsInboundOrder.Delete")]
    public Task<WebResponseContent> Del([FromBody] JsonElement ids, CancellationToken ct) =>
        EntityCrudController.Del(crud, ids, ct);

    [HttpPost("export")]
    [Permission("WmsInboundOrder.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

    [HttpGet("exportTemplate")]
    [Permission("WmsInboundOrder.Import")]
    public IActionResult ExportTemplate() =>
        EntityCrudExcel.ExportTemplate(this, crud);

    [HttpPost("import")]
    [Permission("WmsInboundOrder.Import")]
    [RequestSizeLimit(50_000_000)]
    public Task<WebResponseContent> Import(IFormFile file, CancellationToken ct) =>
        EntityCrudExcel.ImportAsync(crud, file, ct);

}

[Route("api/WmsOutboundOrderLine")]
[ApiController]
[Authorize]
public class WmsOutboundOrderLineController(EntityCrudService<WmsOutboundOrderLine> crud) : ControllerBase
{
    [HttpPost("getPageData")]
    [Permission("WmsOutboundOrder.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudController.Page(crud, options, ct);

    [HttpPost("add")]
    [Permission("WmsOutboundOrder.Add")]
    public Task<WebResponseContent> Add([FromBody] WmsOutboundOrderLine entity, CancellationToken ct) =>
        EntityCrudController.Add(crud, entity, ct);

    [HttpPost("update")]
    [Permission("WmsOutboundOrder.Update")]
    public Task<WebResponseContent> Update([FromBody] WmsOutboundOrderLine entity, CancellationToken ct) =>
        EntityCrudController.Update(crud, entity, ct);

    [HttpPost("del")]
    [Permission("WmsOutboundOrder.Delete")]
    public Task<WebResponseContent> Del([FromBody] JsonElement ids, CancellationToken ct) =>
        EntityCrudController.Del(crud, ids, ct);

    [HttpPost("export")]
    [Permission("WmsOutboundOrder.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

    [HttpGet("exportTemplate")]
    [Permission("WmsOutboundOrder.Import")]
    public IActionResult ExportTemplate() =>
        EntityCrudExcel.ExportTemplate(this, crud);

    [HttpPost("import")]
    [Permission("WmsOutboundOrder.Import")]
    [RequestSizeLimit(50_000_000)]
    public Task<WebResponseContent> Import(IFormFile file, CancellationToken ct) =>
        EntityCrudExcel.ImportAsync(crud, file, ct);

}

[Route("api/WmsPickingTask")]
[ApiController]
[Authorize]
public class WmsPickingTaskCrudController(EntityCrudService<WmsPickingTask> crud) : ControllerBase
{
    [HttpPost("getPageData")]
    [Permission("WmsPickingTask.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudController.Page(crud, options, ct);

    [HttpPost("add")]
    [Permission("WmsPickingTask.Add")]
    public Task<WebResponseContent> Add([FromBody] WmsPickingTask entity, CancellationToken ct) =>
        EntityCrudController.Add(crud, entity, ct);

    [HttpPost("update")]
    [Permission("WmsPickingTask.Update")]
    public Task<WebResponseContent> Update([FromBody] WmsPickingTask entity, CancellationToken ct) =>
        EntityCrudController.Update(crud, entity, ct);

    [HttpPost("del")]
    [Permission("WmsPickingTask.Delete")]
    public Task<WebResponseContent> Del([FromBody] JsonElement ids, CancellationToken ct) =>
        EntityCrudController.Del(crud, ids, ct);

    [HttpPost("export")]
    [Permission("WmsPickingTask.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

    [HttpGet("exportTemplate")]
    [Permission("WmsPickingTask.Import")]
    public IActionResult ExportTemplate() =>
        EntityCrudExcel.ExportTemplate(this, crud);

    [HttpPost("import")]
    [Permission("WmsPickingTask.Import")]
    [RequestSizeLimit(50_000_000)]
    public Task<WebResponseContent> Import(IFormFile file, CancellationToken ct) =>
        EntityCrudExcel.ImportAsync(crud, file, ct);

}
