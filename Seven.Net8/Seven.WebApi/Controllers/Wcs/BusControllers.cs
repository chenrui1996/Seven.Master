using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seven.Application.Wcs;
using Seven.Domain.Common;
using Seven.Infrastructure.Security;

namespace Seven.WebApi.Controllers.Wcs;

[Route("api/BusTransportOrder")]
[ApiController]
[Authorize]
public class BusTransportOrdersController : ControllerBase
{
    private readonly IBusTransportOrderQuery _orders;

    public BusTransportOrdersController(IBusTransportOrderQuery orders) => _orders = orders;

    [HttpPost("getPageData")]
    [Permission("BusTransportOrder.Search")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _orders.GetPageDataAsync(options, ct));

    [HttpGet("{id:guid}")]
    [Permission("BusTransportOrder.Search")]
    public async Task<WebResponseContent> GetWithLegs(Guid id, CancellationToken ct)
    {
        var row = await _orders.GetWithLegsAsync(id, ct);
        return row == null ? WebResponseContent.Error("运输单不存在") : WebResponseContent.Ok(data: row);
    }
}

[Route("api/BusTransportLeg")]
[ApiController]
[Authorize]
public class BusTransportLegsController : ControllerBase
{
    private readonly IBusTransportOrderQuery _orders;

    public BusTransportLegsController(IBusTransportOrderQuery orders) => _orders = orders;

    [HttpPost("getPageData")]
    [Permission("BusTransportOrder.Search")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _orders.GetLegsPageAsync(options, ct));
}
