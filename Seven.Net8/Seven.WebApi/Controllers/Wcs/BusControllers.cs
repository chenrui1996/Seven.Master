using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seven.Application.Wcs;
using Seven.Domain.Common;
using Seven.Infrastructure.Security;

namespace Seven.WebApi.Controllers.Wcs;

[Route("api/BusTransportOrder")]
[ApiController]
[Authorize]
[RequiresFeature("OrchestrationBus")]
public class BusTransportOrdersController : ControllerBase
{
    private readonly IBusTransportOrderQuery _orders;

    public BusTransportOrdersController(IBusTransportOrderQuery orders) => _orders = orders;

    [HttpPost("getPageData")]
    [Permission("BusTransportOrder.Search")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _orders.GetPageDataAsync(options, ct));
}
