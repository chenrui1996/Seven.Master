using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Seven.Application.Wms;
using Seven.Domain.Common;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Wms;
using Seven.Infrastructure.Security;
using Seven.WebApi.Controllers.Crud;

namespace Seven.WebApi.Controllers.Wms;

[Route("api/WmsLocation")]
[ApiController]
[Authorize]
public class WmsLocationsController : ControllerBase
{
    private readonly ILocationService _locations;

    public WmsLocationsController(ILocationService locations) => _locations = locations;

    [HttpGet("{code}")]
    [Permission("WmsLocation.Search")]
    public async Task<WebResponseContent> GetByCode(string code, CancellationToken ct)
    {
        var loc = await _locations.GetByCodeAsync(code, ct);
        return loc == null ? WebResponseContent.Error("不存在") : WebResponseContent.Ok(data: loc);
    }

    [HttpPost("getPageData")]
    [Permission("WmsLocation.Search")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _locations.GetPageDataAsync(options, ct));

    [HttpPost("add")]
    [Permission("WmsLocation.Add")]
    public Task<WebResponseContent> Add([FromBody] WmsLocation entity, CancellationToken ct) =>
        _locations.AddAsync(entity, ct);

    [HttpPost("update")]
    [Permission("WmsLocation.Update")]
    public Task<WebResponseContent> Update([FromBody] WmsLocation entity, CancellationToken ct) =>
        _locations.UpdateAsync(entity, ct);

    [HttpPost("del")]
    [Permission("WmsLocation.Delete")]
    public Task<WebResponseContent> Del(
        [FromBody] System.Text.Json.JsonElement ids,
        [FromServices] Seven.Infrastructure.Crud.EntityCrudService<WmsLocation> crud,
        CancellationToken ct) =>
        crud.DeleteAsync(ids, ct);

    [HttpPost("export")]
    [Permission("WmsLocation.Export")]
    public Task<IActionResult> Export(
        [FromBody] PageDataOptions options,
        [FromServices] Seven.Infrastructure.Crud.EntityCrudService<WmsLocation> crud,
        CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

    [HttpGet("exportTemplate")]
    [Permission("WmsLocation.Import")]
    public IActionResult ExportTemplate([FromServices] Seven.Infrastructure.Crud.EntityCrudService<WmsLocation> crud) =>
        EntityCrudExcel.ExportTemplate(this, crud);

    [HttpPost("import")]
    [Permission("WmsLocation.Import")]
    [RequestSizeLimit(50_000_000)]
    public Task<WebResponseContent> Import(
        IFormFile file,
        [FromServices] Seven.Infrastructure.Crud.EntityCrudService<WmsLocation> crud,
        CancellationToken ct) =>
        EntityCrudExcel.ImportAsync(crud, file, ct);
}

[Route("api/WmsStock")]
[ApiController]
[Authorize]
public class WmsStocksController : ControllerBase
{
    private readonly IStockService _stocks;

    public WmsStocksController(IStockService stocks) => _stocks = stocks;

    [HttpPost("getPageData")]
    [Permission("WmsStock.Search")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _stocks.GetPageDataAsync(options, ct));

    [HttpPost("receive")]
    [Permission("WmsStock.Update")]
    public async Task<WebResponseContent> Receive([FromBody] ReceiveStockRequest request, CancellationToken ct)
    {
        try
        {
            return WebResponseContent.Ok("操作成功", await _stocks.ReceiveAsync(request, ct));
        }
        catch (WmsDomainException ex)
        {
            return WebResponseContent.Error(ex.Message);
        }
    }

    [HttpPost("ship")]
    [Permission("WmsStock.Update")]
    public async Task<WebResponseContent> Ship([FromBody] ShipStockRequest request, CancellationToken ct)
    {
        try
        {
            return WebResponseContent.Ok("操作成功", await _stocks.ShipAsync(request, ct));
        }
        catch (WmsDomainException ex)
        {
            return WebResponseContent.Error(ex.Message);
        }
    }
}

[Route("api/WmsContainer")]
[ApiController]
[Authorize]
public class WmsContainersController : ControllerBase
{
    private readonly IContainerService _containers;

    public WmsContainersController(IContainerService containers) => _containers = containers;

    [HttpGet("{code}")]
    [Permission("WmsContainer.Search")]
    public async Task<WebResponseContent> GetByCode(string code, CancellationToken ct)
    {
        var row = await _containers.GetByCodeAsync(code, ct);
        return row == null ? WebResponseContent.Error("不存在") : WebResponseContent.Ok(data: row);
    }

    [HttpPost("getPageData")]
    [Permission("WmsContainer.Search")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _containers.GetPageDataAsync(options, ct));

    [HttpPost("add")]
    [Permission("WmsContainer.Add")]
    public Task<WebResponseContent> Add([FromBody] WmsContainer entity, CancellationToken ct) =>
        _containers.AddAsync(entity, ct);

    [HttpPost("update")]
    [Permission("WmsContainer.Update")]
    public Task<WebResponseContent> Update([FromBody] WmsContainer entity, CancellationToken ct) =>
        _containers.UpdateAsync(entity, ct);

    [HttpPost("del")]
    [Permission("WmsContainer.Delete")]
    public Task<WebResponseContent> Del(
        [FromBody] System.Text.Json.JsonElement ids,
        [FromServices] Seven.Infrastructure.Crud.EntityCrudService<WmsContainer> crud,
        CancellationToken ct) =>
        crud.DeleteAsync(ids, ct);

    [HttpPost("export")]
    [Permission("WmsContainer.Export")]
    public Task<IActionResult> Export(
        [FromBody] PageDataOptions options,
        [FromServices] Seven.Infrastructure.Crud.EntityCrudService<WmsContainer> crud,
        CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

    [HttpGet("exportTemplate")]
    [Permission("WmsContainer.Import")]
    public IActionResult ExportTemplate([FromServices] Seven.Infrastructure.Crud.EntityCrudService<WmsContainer> crud) =>
        EntityCrudExcel.ExportTemplate(this, crud);

    [HttpPost("import")]
    [Permission("WmsContainer.Import")]
    [RequestSizeLimit(50_000_000)]
    public Task<WebResponseContent> Import(
        IFormFile file,
        [FromServices] Seven.Infrastructure.Crud.EntityCrudService<WmsContainer> crud,
        CancellationToken ct) =>
        EntityCrudExcel.ImportAsync(crud, file, ct);
}

[Route("api/WmsInboundOrder")]
[ApiController]
[Authorize]
public class WmsInboundOrdersController : ControllerBase
{
    private readonly IInboundOrderService _orders;

    public WmsInboundOrdersController(IInboundOrderService orders) => _orders = orders;

    [HttpPost("getPageData")]
    [Permission("WmsInboundOrder.Search")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _orders.GetPageDataAsync(options, ct));

    [HttpGet("{id:int}")]
    [Permission("WmsInboundOrder.Search")]
    public async Task<WebResponseContent> Get(int id, CancellationToken ct)
    {
        var order = await _orders.GetAsync(id, ct);
        return order == null ? WebResponseContent.Error("入库单不存在") : WebResponseContent.Ok(data: order);
    }

    [HttpPost("add")]
    [Permission("WmsInboundOrder.Add")]
    public async Task<WebResponseContent> Create([FromBody] CreateInboundOrderRequest request, CancellationToken ct)
    {
        try
        {
            return WebResponseContent.Ok("操作成功", await _orders.CreateAsync(request, ct));
        }
        catch (WmsDomainException ex)
        {
            return WebResponseContent.Error(ex.Message);
        }
    }

    [HttpPost("approve/{id:int}")]
    [Permission("WmsInboundOrder.Update")]
    public async Task<WebResponseContent> Approve(int id, CancellationToken ct)
    {
        try
        {
            await _orders.ApproveAsync(id, ct);
            return WebResponseContent.Ok("操作成功");
        }
        catch (WmsDomainException ex)
        {
            return WebResponseContent.Error(ex.Message);
        }
    }

    [HttpPost("receive/{id:int}")]
    [Permission("WmsInboundOrder.Update")]
    public async Task<WebResponseContent> Receive(
        int id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ReceiveAndBuildPalletRequest? request,
        CancellationToken ct)
    {
        try
        {
            await _orders.ReceiveAndBuildPalletAsync(id, request, ct);
            return WebResponseContent.Ok("操作成功");
        }
        catch (WmsDomainException ex)
        {
            return WebResponseContent.Error(ex.Message);
        }
    }

    [HttpPost("buildPallet/{id:int}")]
    [Permission("WmsInboundOrder.Update")]
    public async Task<WebResponseContent> BuildPallet(
        int id,
        [FromBody] BuildPalletRequest request,
        CancellationToken ct)
    {
        try
        {
            return WebResponseContent.Ok("组盘成功", await _orders.BuildPalletAsync(id, request, ct));
        }
        catch (WmsDomainException ex)
        {
            return WebResponseContent.Error(ex.Message);
        }
    }
}

[Route("api/WmsOutboundOrder")]
[ApiController]
[Authorize]
public class WmsOutboundOrdersController : ControllerBase
{
    private readonly IOutboundOrderService _orders;

    public WmsOutboundOrdersController(IOutboundOrderService orders) => _orders = orders;

    [HttpPost("getPageData")]
    [Permission("WmsOutboundOrder.Search")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _orders.GetPageDataAsync(options, ct));

    [HttpGet("{id:int}")]
    [Permission("WmsOutboundOrder.Search")]
    public async Task<WebResponseContent> Get(int id, CancellationToken ct)
    {
        var order = await _orders.GetAsync(id, ct);
        return order == null ? WebResponseContent.Error("出库单不存在") : WebResponseContent.Ok(data: order);
    }

    [HttpPost("add")]
    [Permission("WmsOutboundOrder.Add")]
    public async Task<WebResponseContent> Create([FromBody] CreateOutboundOrderRequest request, CancellationToken ct)
    {
        try
        {
            return WebResponseContent.Ok("操作成功", await _orders.CreateAsync(request, ct));
        }
        catch (WmsDomainException ex)
        {
            return WebResponseContent.Error(ex.Message);
        }
    }

    [HttpPost("approve/{id:int}")]
    [Permission("WmsOutboundOrder.Update")]
    public async Task<WebResponseContent> Approve(int id, CancellationToken ct)
    {
        try
        {
            await _orders.ApproveAsync(id, ct);
            return WebResponseContent.Ok("操作成功");
        }
        catch (WmsDomainException ex)
        {
            return WebResponseContent.Error(ex.Message);
        }
    }

    [HttpPost("ship/{id:int}")]
    [Permission("WmsOutboundOrder.Update")]
    public async Task<WebResponseContent> Ship(int id, CancellationToken ct)
    {
        try
        {
            await _orders.ShipAsync(id, ct);
            return WebResponseContent.Ok("操作成功");
        }
        catch (WmsDomainException ex)
        {
            return WebResponseContent.Error(ex.Message, ex.Code);
        }
    }

    [HttpPost("generatePicks/{id:int}")]
    [Permission("WmsOutboundOrder.Update")]
    public async Task<WebResponseContent> GeneratePicks(
        int id,
        [FromServices] IPickingService picking,
        CancellationToken ct)
    {
        try
        {
            return WebResponseContent.Ok("操作成功", await picking.GenerateFromOutboundAsync(id, ct));
        }
        catch (WmsDomainException ex)
        {
            return WebResponseContent.Error(ex.Message, ex.Code);
        }
    }
}

[Route("api/WmsPickingTask")]
[ApiController]
[Authorize]
public class WmsPickingTasksOpsController : ControllerBase
{
    private readonly IPickingService _picking;

    public WmsPickingTasksOpsController(IPickingService picking) => _picking = picking;

    [HttpPost("confirmPick")]
    [Permission("WmsPickingTask.Update")]
    public async Task<WebResponseContent> ConfirmPick([FromBody] ConfirmPickRequest request, CancellationToken ct)
    {
        try
        {
            return WebResponseContent.Ok("操作成功", await _picking.ConfirmPickAsync(request, ct));
        }
        catch (WmsDomainException ex)
        {
            return WebResponseContent.Error(ex.Message, ex.Code);
        }
    }

    [HttpPost("cancel/{id:int}")]
    [Permission("WmsPickingTask.Update")]
    public async Task<WebResponseContent> Cancel(int id, CancellationToken ct)
    {
        try
        {
            await _picking.CancelAsync(id, ct);
            return WebResponseContent.Ok("操作成功");
        }
        catch (WmsDomainException ex)
        {
            return WebResponseContent.Error(ex.Message, ex.Code);
        }
    }

    [HttpGet("pending")]
    [Permission("WmsPickingTask.Search")]
    public async Task<WebResponseContent> Pending(CancellationToken ct) =>
        WebResponseContent.Ok(data: await _picking.ListPendingAsync(ct));
}

[Route("api/WmsCycleCount")]
[ApiController]
[Authorize]
public class WmsCycleCountsController : ControllerBase
{
    private readonly ICycleCountService _orders;

    public WmsCycleCountsController(ICycleCountService orders) => _orders = orders;

    [HttpPost("getPageData")]
    [Permission("WmsCycleCount.Search")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _orders.GetPageDataAsync(options, ct));

    [HttpPost("get/{id:int}")]
    [Permission("WmsCycleCount.Search")]
    public async Task<WebResponseContent> Get(int id, CancellationToken ct)
    {
        var order = await _orders.GetAsync(id, ct);
        return order == null ? WebResponseContent.Error("盘点单不存在") : WebResponseContent.Ok(data: order);
    }

    [HttpPost("add")]
    [Permission("WmsCycleCount.Add")]
    public async Task<WebResponseContent> CreatePlan([FromBody] CreateCycleCountRequest request, CancellationToken ct)
    {
        try
        {
            return WebResponseContent.Ok("操作成功", await _orders.CreatePlanAsync(request, ct));
        }
        catch (WmsDomainException ex)
        {
            return WebResponseContent.Error(ex.Message);
        }
    }

    [HttpPost("record/{id:int}/{lineNo:int}")]
    [Permission("WmsCycleCount.Update")]
    public async Task<WebResponseContent> RecordCount(int id, int lineNo, [FromBody] decimal countQty, CancellationToken ct)
    {
        try
        {
            await _orders.RecordCountAsync(id, lineNo, countQty, ct);
            return WebResponseContent.Ok("操作成功");
        }
        catch (WmsDomainException ex)
        {
            return WebResponseContent.Error(ex.Message);
        }
    }

    [HttpPost("confirm/{id:int}")]
    [Permission("WmsCycleCount.Update")]
    public async Task<WebResponseContent> ConfirmAdjust(int id, CancellationToken ct)
    {
        try
        {
            await _orders.ConfirmAdjustAsync(id, ct);
            return WebResponseContent.Ok("操作成功");
        }
        catch (WmsDomainException ex)
        {
            return WebResponseContent.Error(ex.Message);
        }
    }
}
