using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seven.Domain.Common;
using Seven.Domain.Entities.Wcs.FourWay;
using Seven.Infrastructure.Crud;
using Seven.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Seven.WebApi.Controllers.Crud;

namespace Seven.WebApi.Controllers.Wcs.FourWay;

file static class FwCrud
{
    public static async Task<WebResponseContent> Page<T>(EntityCrudService<T> crud, PageDataOptions o, CancellationToken ct) where T : class, new() =>
        WebResponseContent.Ok(data: await crud.GetPageDataAsync(o, ct));
    public static Task<WebResponseContent> Add<T>(EntityCrudService<T> crud, T e, CancellationToken ct) where T : class, new() => crud.AddAsync(e, ct);
    public static Task<WebResponseContent> Update<T>(EntityCrudService<T> crud, T e, CancellationToken ct) where T : class, new() => crud.UpdateAsync(e, ct);
    public static Task<WebResponseContent> Del<T>(EntityCrudService<T> crud, JsonElement ids, CancellationToken ct) where T : class, new() => crud.DeleteAsync(ids, ct);
}

[Route("api/FwLayerPolicy")]
[ApiController]
[Authorize]
public class FwLayerPolicyController(EntityCrudService<FwLayerPolicy> crud) : ControllerBase
{
    [HttpPost("getPageData")][Permission("FwLayerPolicy.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions o, CancellationToken ct) => FwCrud.Page(crud, o, ct);
    [HttpPost("add")][Permission("FwLayerPolicy.Add")]
    public Task<WebResponseContent> Add([FromBody] FwLayerPolicy e, CancellationToken ct) => FwCrud.Add(crud, e, ct);
    [HttpPost("update")][Permission("FwLayerPolicy.Update")]
    public Task<WebResponseContent> Update([FromBody] FwLayerPolicy e, CancellationToken ct) => FwCrud.Update(crud, e, ct);
    [HttpPost("del")][Permission("FwLayerPolicy.Delete")]
    public Task<WebResponseContent> Del([FromBody] JsonElement ids, CancellationToken ct) => FwCrud.Del(crud, ids, ct);

    [HttpPost("export")]
    [Permission("FwLayerPolicy.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

    [HttpGet("exportTemplate")]
    [Permission("FwLayerPolicy.Import")]
    public IActionResult ExportTemplate() =>
        EntityCrudExcel.ExportTemplate(this, crud);

    [HttpPost("import")]
    [Permission("FwLayerPolicy.Import")]
    [RequestSizeLimit(50_000_000)]
    public Task<WebResponseContent> Import(IFormFile file, CancellationToken ct) =>
        EntityCrudExcel.ImportAsync(crud, file, ct);

}

[Route("api/FwAislePolicy")]
[ApiController]
[Authorize]
public class FwAislePolicyController(EntityCrudService<FwAislePolicy> crud) : ControllerBase
{
    [HttpPost("getPageData")][Permission("FwAislePolicy.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions o, CancellationToken ct) => FwCrud.Page(crud, o, ct);
    [HttpPost("add")][Permission("FwAislePolicy.Add")]
    public Task<WebResponseContent> Add([FromBody] FwAislePolicy e, CancellationToken ct) => FwCrud.Add(crud, e, ct);
    [HttpPost("update")][Permission("FwAislePolicy.Update")]
    public Task<WebResponseContent> Update([FromBody] FwAislePolicy e, CancellationToken ct) => FwCrud.Update(crud, e, ct);
    [HttpPost("del")][Permission("FwAislePolicy.Delete")]
    public Task<WebResponseContent> Del([FromBody] JsonElement ids, CancellationToken ct) => FwCrud.Del(crud, ids, ct);

    [HttpPost("export")]
    [Permission("FwAislePolicy.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

    [HttpGet("exportTemplate")]
    [Permission("FwAislePolicy.Import")]
    public IActionResult ExportTemplate() =>
        EntityCrudExcel.ExportTemplate(this, crud);

    [HttpPost("import")]
    [Permission("FwAislePolicy.Import")]
    [RequestSizeLimit(50_000_000)]
    public Task<WebResponseContent> Import(IFormFile file, CancellationToken ct) =>
        EntityCrudExcel.ImportAsync(crud, file, ct);

}

[Route("api/FwRequestPoint")]
[ApiController]
[Authorize]
public class FwRequestPointController(EntityCrudService<FwRequestPoint> crud) : ControllerBase
{
    [HttpPost("getPageData")][Permission("FwRequestPoint.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions o, CancellationToken ct) => FwCrud.Page(crud, o, ct);
    [HttpPost("add")][Permission("FwRequestPoint.Add")]
    public Task<WebResponseContent> Add([FromBody] FwRequestPoint e, CancellationToken ct) => FwCrud.Add(crud, e, ct);
    [HttpPost("update")][Permission("FwRequestPoint.Update")]
    public Task<WebResponseContent> Update([FromBody] FwRequestPoint e, CancellationToken ct) => FwCrud.Update(crud, e, ct);
    [HttpPost("del")][Permission("FwRequestPoint.Delete")]
    public Task<WebResponseContent> Del([FromBody] JsonElement ids, CancellationToken ct) => FwCrud.Del(crud, ids, ct);

    [HttpPost("export")]
    [Permission("FwRequestPoint.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

    [HttpGet("exportTemplate")]
    [Permission("FwRequestPoint.Import")]
    public IActionResult ExportTemplate() =>
        EntityCrudExcel.ExportTemplate(this, crud);

    [HttpPost("import")]
    [Permission("FwRequestPoint.Import")]
    [RequestSizeLimit(50_000_000)]
    public Task<WebResponseContent> Import(IFormFile file, CancellationToken ct) =>
        EntityCrudExcel.ImportAsync(crud, file, ct);

}

[Route("api/FwMapVersion")]
[ApiController]
[Authorize]
public class FwMapVersionController(EntityCrudService<FwMapVersion> crud) : ControllerBase
{
    [HttpPost("getPageData")][Permission("FwMapVersion.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions o, CancellationToken ct) => FwCrud.Page(crud, o, ct);
    [HttpPost("add")][Permission("FwMapVersion.Add")]
    public Task<WebResponseContent> Add([FromBody] FwMapVersion e, CancellationToken ct) => FwCrud.Add(crud, e, ct);
    [HttpPost("update")][Permission("FwMapVersion.Update")]
    public Task<WebResponseContent> Update([FromBody] FwMapVersion e, CancellationToken ct) => FwCrud.Update(crud, e, ct);
    [HttpPost("del")][Permission("FwMapVersion.Delete")]
    public Task<WebResponseContent> Del([FromBody] JsonElement ids, CancellationToken ct) => FwCrud.Del(crud, ids, ct);

    [HttpPost("export")]
    [Permission("FwMapVersion.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

    [HttpGet("exportTemplate")]
    [Permission("FwMapVersion.Import")]
    public IActionResult ExportTemplate() =>
        EntityCrudExcel.ExportTemplate(this, crud);

    [HttpPost("import")]
    [Permission("FwMapVersion.Import")]
    [RequestSizeLimit(50_000_000)]
    public Task<WebResponseContent> Import(IFormFile file, CancellationToken ct) =>
        EntityCrudExcel.ImportAsync(crud, file, ct);

}

[Route("api/FwNode")]
[ApiController]
[Authorize]
public class FwNodeController(EntityCrudService<FwNode> crud) : ControllerBase
{
    [HttpPost("getPageData")][Permission("FwNode.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions o, CancellationToken ct) => FwCrud.Page(crud, o, ct);
    [HttpPost("add")][Permission("FwNode.Add")]
    public Task<WebResponseContent> Add([FromBody] FwNode e, CancellationToken ct) => FwCrud.Add(crud, e, ct);
    [HttpPost("update")][Permission("FwNode.Update")]
    public Task<WebResponseContent> Update([FromBody] FwNode e, CancellationToken ct) => FwCrud.Update(crud, e, ct);
    [HttpPost("del")][Permission("FwNode.Delete")]
    public Task<WebResponseContent> Del([FromBody] JsonElement ids, CancellationToken ct) => FwCrud.Del(crud, ids, ct);

    [HttpPost("export")]
    [Permission("FwNode.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

    [HttpGet("exportTemplate")]
    [Permission("FwNode.Import")]
    public IActionResult ExportTemplate() =>
        EntityCrudExcel.ExportTemplate(this, crud);

    [HttpPost("import")]
    [Permission("FwNode.Import")]
    [RequestSizeLimit(50_000_000)]
    public Task<WebResponseContent> Import(IFormFile file, CancellationToken ct) =>
        EntityCrudExcel.ImportAsync(crud, file, ct);

}

[Route("api/FwRoute")]
[ApiController]
[Authorize]
public class FwRouteController(EntityCrudService<FwRoute> crud) : ControllerBase
{
    [HttpPost("getPageData")][Permission("FwRoute.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions o, CancellationToken ct) => FwCrud.Page(crud, o, ct);
    [HttpPost("add")][Permission("FwRoute.Add")]
    public Task<WebResponseContent> Add([FromBody] FwRoute e, CancellationToken ct) => FwCrud.Add(crud, e, ct);
    [HttpPost("update")][Permission("FwRoute.Update")]
    public Task<WebResponseContent> Update([FromBody] FwRoute e, CancellationToken ct) => FwCrud.Update(crud, e, ct);
    [HttpPost("del")][Permission("FwRoute.Delete")]
    public Task<WebResponseContent> Del([FromBody] JsonElement ids, CancellationToken ct) => FwCrud.Del(crud, ids, ct);

    [HttpPost("export")]
    [Permission("FwRoute.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

    [HttpGet("exportTemplate")]
    [Permission("FwRoute.Import")]
    public IActionResult ExportTemplate() =>
        EntityCrudExcel.ExportTemplate(this, crud);

    [HttpPost("import")]
    [Permission("FwRoute.Import")]
    [RequestSizeLimit(50_000_000)]
    public Task<WebResponseContent> Import(IFormFile file, CancellationToken ct) =>
        EntityCrudExcel.ImportAsync(crud, file, ct);

}

[Route("api/FwParkingLedger")]
[ApiController]
[Authorize]
public class FwParkingLedgerController(EntityCrudService<FwParkingLedger> crud) : ControllerBase
{
    [HttpPost("getPageData")][Permission("FwParkingLedger.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions o, CancellationToken ct) => FwCrud.Page(crud, o, ct);
    [HttpPost("add")][Permission("FwParkingLedger.Add")]
    public Task<WebResponseContent> Add([FromBody] FwParkingLedger e, CancellationToken ct) => FwCrud.Add(crud, e, ct);
    [HttpPost("update")][Permission("FwParkingLedger.Update")]
    public Task<WebResponseContent> Update([FromBody] FwParkingLedger e, CancellationToken ct) => FwCrud.Update(crud, e, ct);
    [HttpPost("del")][Permission("FwParkingLedger.Delete")]
    public Task<WebResponseContent> Del([FromBody] JsonElement ids, CancellationToken ct) => FwCrud.Del(crud, ids, ct);

    [HttpPost("export")]
    [Permission("FwParkingLedger.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

    [HttpGet("exportTemplate")]
    [Permission("FwParkingLedger.Import")]
    public IActionResult ExportTemplate() =>
        EntityCrudExcel.ExportTemplate(this, crud);

    [HttpPost("import")]
    [Permission("FwParkingLedger.Import")]
    [RequestSizeLimit(50_000_000)]
    public Task<WebResponseContent> Import(IFormFile file, CancellationToken ct) =>
        EntityCrudExcel.ImportAsync(crud, file, ct);

}

[Route("api/FwHoistDevice")]
[ApiController]
[Authorize]
public class FwHoistDeviceController(EntityCrudService<FwHoistDevice> crud) : ControllerBase
{
    [HttpPost("getPageData")][Permission("FwHoistDevice.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions o, CancellationToken ct) => FwCrud.Page(crud, o, ct);
    [HttpPost("add")][Permission("FwHoistDevice.Add")]
    public Task<WebResponseContent> Add([FromBody] FwHoistDevice e, CancellationToken ct) => FwCrud.Add(crud, e, ct);
    [HttpPost("update")][Permission("FwHoistDevice.Update")]
    public Task<WebResponseContent> Update([FromBody] FwHoistDevice e, CancellationToken ct) => FwCrud.Update(crud, e, ct);
    [HttpPost("del")][Permission("FwHoistDevice.Delete")]
    public Task<WebResponseContent> Del([FromBody] JsonElement ids, CancellationToken ct) => FwCrud.Del(crud, ids, ct);

    [HttpPost("export")]
    [Permission("FwHoistDevice.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

    [HttpGet("exportTemplate")]
    [Permission("FwHoistDevice.Import")]
    public IActionResult ExportTemplate() =>
        EntityCrudExcel.ExportTemplate(this, crud);

    [HttpPost("import")]
    [Permission("FwHoistDevice.Import")]
    [RequestSizeLimit(50_000_000)]
    public Task<WebResponseContent> Import(IFormFile file, CancellationToken ct) =>
        EntityCrudExcel.ImportAsync(crud, file, ct);

}

[Route("api/FwHoistLayerPoint")]
[ApiController]
[Authorize]
public class FwHoistLayerPointController(EntityCrudService<FwHoistLayerPoint> crud) : ControllerBase
{
    [HttpPost("getPageData")][Permission("FwHoistLayerPoint.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions o, CancellationToken ct) => FwCrud.Page(crud, o, ct);
    [HttpPost("add")][Permission("FwHoistLayerPoint.Add")]
    public Task<WebResponseContent> Add([FromBody] FwHoistLayerPoint e, CancellationToken ct) => FwCrud.Add(crud, e, ct);
    [HttpPost("update")][Permission("FwHoistLayerPoint.Update")]
    public Task<WebResponseContent> Update([FromBody] FwHoistLayerPoint e, CancellationToken ct) => FwCrud.Update(crud, e, ct);
    [HttpPost("del")][Permission("FwHoistLayerPoint.Delete")]
    public Task<WebResponseContent> Del([FromBody] JsonElement ids, CancellationToken ct) => FwCrud.Del(crud, ids, ct);

    [HttpPost("export")]
    [Permission("FwHoistLayerPoint.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

    [HttpGet("exportTemplate")]
    [Permission("FwHoistLayerPoint.Import")]
    public IActionResult ExportTemplate() =>
        EntityCrudExcel.ExportTemplate(this, crud);

    [HttpPost("import")]
    [Permission("FwHoistLayerPoint.Import")]
    [RequestSizeLimit(50_000_000)]
    public Task<WebResponseContent> Import(IFormFile file, CancellationToken ct) =>
        EntityCrudExcel.ImportAsync(crud, file, ct);

}

[Route("api/FwPutAwayTask")]
[ApiController]
[Authorize]
public class FwPutAwayTaskController(EntityCrudService<FwPutAwayTask> crud) : ControllerBase
{
    [HttpPost("getPageData")][Permission("FwPutAwayTask.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions o, CancellationToken ct) => FwCrud.Page(crud, o, ct);
    [HttpPost("update")][Permission("FwPutAwayTask.Update")]
    public Task<WebResponseContent> Update([FromBody] FwPutAwayTask e, CancellationToken ct) => FwCrud.Update(crud, e, ct);

    [HttpPost("export")]
    [Permission("FwPutAwayTask.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

}

[Route("api/FwRetrievalTask")]
[ApiController]
[Authorize]
public class FwRetrievalTaskController(EntityCrudService<FwRetrievalTask> crud) : ControllerBase
{
    [HttpPost("getPageData")][Permission("FwRetrievalTask.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions o, CancellationToken ct) => FwCrud.Page(crud, o, ct);
    [HttpPost("update")][Permission("FwRetrievalTask.Update")]
    public Task<WebResponseContent> Update([FromBody] FwRetrievalTask e, CancellationToken ct) => FwCrud.Update(crud, e, ct);

    [HttpPost("export")]
    [Permission("FwRetrievalTask.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

}

[Route("api/FwShuttleTask")]
[ApiController]
[Authorize]
public class FwShuttleTaskController(EntityCrudService<FwShuttleTask> crud) : ControllerBase
{
    [HttpPost("getPageData")][Permission("FwShuttleTask.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions o, CancellationToken ct) => FwCrud.Page(crud, o, ct);
    [HttpPost("update")][Permission("FwShuttleTask.Update")]
    public Task<WebResponseContent> Update([FromBody] FwShuttleTask e, CancellationToken ct) => FwCrud.Update(crud, e, ct);

    [HttpPost("export")]
    [Permission("FwShuttleTask.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

}

[Route("api/FwHoistTask")]
[ApiController]
[Authorize]
public class FwHoistTaskController(EntityCrudService<FwHoistTask> crud) : ControllerBase
{
    [HttpPost("getPageData")][Permission("FwHoistTask.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions o, CancellationToken ct) => FwCrud.Page(crud, o, ct);
    [HttpPost("update")][Permission("FwHoistTask.Update")]
    public Task<WebResponseContent> Update([FromBody] FwHoistTask e, CancellationToken ct) => FwCrud.Update(crud, e, ct);

    [HttpPost("export")]
    [Permission("FwHoistTask.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

}

[Route("api/FwHoistExecTask")]
[ApiController]
[Authorize]
public class FwHoistExecTaskController(EntityCrudService<FwHoistExecTask> crud) : ControllerBase
{
    [HttpPost("getPageData")][Permission("FwHoistExecTask.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions o, CancellationToken ct) => FwCrud.Page(crud, o, ct);
    [HttpPost("update")][Permission("FwHoistExecTask.Update")]
    public Task<WebResponseContent> Update([FromBody] FwHoistExecTask e, CancellationToken ct) => FwCrud.Update(crud, e, ct);

    [HttpPost("export")]
    [Permission("FwHoistExecTask.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

}
