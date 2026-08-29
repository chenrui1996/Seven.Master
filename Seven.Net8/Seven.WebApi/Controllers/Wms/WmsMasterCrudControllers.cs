using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seven.Domain.Common;
using Seven.Domain.Entities.Wms;
using Seven.Infrastructure.Crud;
using Seven.Infrastructure.Security;

namespace Seven.WebApi.Controllers.Wms;

file static class EntityCrudController
{
    public static async Task<WebResponseContent> Page<T>(EntityCrudService<T> crud, PageDataOptions options, CancellationToken ct)
        where T : class =>
        WebResponseContent.Ok(data: await crud.GetPageDataAsync(options, ct));

    public static Task<WebResponseContent> Add<T>(EntityCrudService<T> crud, T entity, CancellationToken ct)
        where T : class =>
        crud.AddAsync(entity, ct);

    public static Task<WebResponseContent> Update<T>(EntityCrudService<T> crud, T entity, CancellationToken ct)
        where T : class =>
        crud.UpdateAsync(entity, ct);

    public static Task<WebResponseContent> Del<T>(EntityCrudService<T> crud, JsonElement ids, CancellationToken ct)
        where T : class =>
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
}
