using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Seven.Application.Wms;
using Seven.Domain.Common;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Wms;
using Seven.Infrastructure.Security;

namespace Seven.WebApi.Controllers.Wms;

[Route("api/WmsLocation")]
[ApiController]
[Authorize]
[RequiresFeature("Wms")]
public class WmsLocationsController : ControllerBase
{
    private readonly ILocationService _locations;

    public WmsLocationsController(ILocationService locations) => _locations = locations;

    [HttpGet("{code}")]
    [Permission("WmsLocation.Search")]
    public async Task<WebResponseContent> GetByCode(string code, CancellationToken ct)
    {
        var loc = await _locations.GetByCodeAsync(code, ct);
        return loc == null ? WebResponseContent.Error("库位不存在") : WebResponseContent.Ok(data: loc);
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
}

[Route("api/WmsStock")]
[ApiController]
[Authorize]
[RequiresFeature("Wms")]
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
            return WebResponseContent.Ok("收货成功", await _stocks.ReceiveAsync(request, ct));
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
            return WebResponseContent.Ok("发运成功", await _stocks.ShipAsync(request, ct));
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
[RequiresFeature("Wms")]
public class WmsContainersController : ControllerBase
{
    private readonly IContainerService _containers;

    public WmsContainersController(IContainerService containers) => _containers = containers;

    [HttpGet("{code}")]
    [Permission("WmsContainer.Search")]
    public async Task<WebResponseContent> GetByCode(string code, CancellationToken ct)
    {
        var row = await _containers.GetByCodeAsync(code, ct);
        return row == null ? WebResponseContent.Error("容器不存在") : WebResponseContent.Ok(data: row);
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
}

[Route("api/WmsInboundOrder")]
[ApiController]
[Authorize]
[RequiresFeature("Wms")]
public class WmsInboundOrdersController : ControllerBase
{
    private readonly IInboundOrderService _orders;

    public WmsInboundOrdersController(IInboundOrderService orders) => _orders = orders;

    [HttpPost("getPageData")]
    [Permission("WmsInboundOrder.Search")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _orders.GetPageDataAsync(options, ct));

    [HttpPost("add")]
    [Permission("WmsInboundOrder.Add")]
    public async Task<WebResponseContent> Create([FromBody] CreateInboundOrderRequest request, CancellationToken ct)
    {
        try
        {
            return WebResponseContent.Ok("创建成功", await _orders.CreateAsync(request, ct));
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
            return WebResponseContent.Ok("审核成功");
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
            return WebResponseContent.Ok("收货组盘成功");
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
[RequiresFeature("Wms")]
public class WmsOutboundOrdersController : ControllerBase
{
    private readonly IOutboundOrderService _orders;

    public WmsOutboundOrdersController(IOutboundOrderService orders) => _orders = orders;

    [HttpPost("getPageData")]
    [Permission("WmsOutboundOrder.Search")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _orders.GetPageDataAsync(options, ct));

    [HttpPost("add")]
    [Permission("WmsOutboundOrder.Add")]
    public async Task<WebResponseContent> Create([FromBody] CreateOutboundOrderRequest request, CancellationToken ct)
    {
        try
        {
            return WebResponseContent.Ok("创建成功", await _orders.CreateAsync(request, ct));
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
            return WebResponseContent.Ok("审核成功");
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
            return WebResponseContent.Ok("发运成功");
        }
        catch (WmsDomainException ex)
        {
            return WebResponseContent.Error(ex.Message);
        }
    }
}

[Route("api/WmsCycleCount")]
[ApiController]
[Authorize]
[RequiresFeature("Wms")]
public class WmsCycleCountsController : ControllerBase
{
    private readonly ICycleCountService _orders;

    public WmsCycleCountsController(ICycleCountService orders) => _orders = orders;

    [HttpPost("getPageData")]
    [Permission("WmsCycleCount.Search")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _orders.GetPageDataAsync(options, ct));

    [HttpPost("add")]
    [Permission("WmsCycleCount.Add")]
    public async Task<WebResponseContent> CreatePlan([FromBody] CreateCycleCountRequest request, CancellationToken ct)
    {
        try
        {
            return WebResponseContent.Ok("创建成功", await _orders.CreatePlanAsync(request, ct));
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
            return WebResponseContent.Ok("实盘录入成功");
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
            return WebResponseContent.Ok("调账成功");
        }
        catch (WmsDomainException ex)
        {
            return WebResponseContent.Error(ex.Message);
        }
    }
}
