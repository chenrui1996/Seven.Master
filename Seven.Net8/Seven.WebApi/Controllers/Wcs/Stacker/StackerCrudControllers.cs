using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seven.Domain.Common;
using Seven.Domain.Entities.Wcs.Stacker;
using Seven.Infrastructure.Crud;
using Seven.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Seven.WebApi.Controllers.Crud;

namespace Seven.WebApi.Controllers.Wcs.Stacker;

file static class StkCrud
{
    public static async Task<WebResponseContent> Page<T>(EntityCrudService<T> crud, PageDataOptions o, CancellationToken ct) where T : class, new() =>
        WebResponseContent.Ok(data: await crud.GetPageDataAsync(o, ct));
    public static Task<WebResponseContent> Add<T>(EntityCrudService<T> crud, T e, CancellationToken ct) where T : class, new() => crud.AddAsync(e, ct);
    public static Task<WebResponseContent> Update<T>(EntityCrudService<T> crud, T e, CancellationToken ct) where T : class, new() => crud.UpdateAsync(e, ct);
    public static Task<WebResponseContent> Del<T>(EntityCrudService<T> crud, JsonElement ids, CancellationToken ct) where T : class, new() => crud.DeleteAsync(ids, ct);
}

[Route("api/StkRequestPoint")]
[ApiController]
[Authorize]
public class StkRequestPointController(EntityCrudService<StkRequestPoint> crud) : ControllerBase
{
    [HttpPost("getPageData")][Permission("StkRequestPoint.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions o, CancellationToken ct) => StkCrud.Page(crud, o, ct);
    [HttpPost("add")][Permission("StkRequestPoint.Add")]
    public Task<WebResponseContent> Add([FromBody] StkRequestPoint e, CancellationToken ct) => StkCrud.Add(crud, e, ct);
    [HttpPost("update")][Permission("StkRequestPoint.Update")]
    public Task<WebResponseContent> Update([FromBody] StkRequestPoint e, CancellationToken ct) => StkCrud.Update(crud, e, ct);
    [HttpPost("del")][Permission("StkRequestPoint.Delete")]
    public Task<WebResponseContent> Del([FromBody] JsonElement ids, CancellationToken ct) => StkCrud.Del(crud, ids, ct);

    [HttpPost("export")]
    [Permission("StkRequestPoint.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

    [HttpGet("exportTemplate")]
    [Permission("StkRequestPoint.Import")]
    public IActionResult ExportTemplate() =>
        EntityCrudExcel.ExportTemplate(this, crud);

    [HttpPost("import")]
    [Permission("StkRequestPoint.Import")]
    [RequestSizeLimit(50_000_000)]
    public Task<WebResponseContent> Import(IFormFile file, CancellationToken ct) =>
        EntityCrudExcel.ImportAsync(crud, file, ct);

}

[Route("api/StkAssignmentPolicy")]
[ApiController]
[Authorize]
public class StkAssignmentPolicyController(EntityCrudService<StkAssignmentPolicy> crud) : ControllerBase
{
    [HttpPost("getPageData")][Permission("StkAssignmentPolicy.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions o, CancellationToken ct) => StkCrud.Page(crud, o, ct);
    [HttpPost("add")][Permission("StkAssignmentPolicy.Add")]
    public Task<WebResponseContent> Add([FromBody] StkAssignmentPolicy e, CancellationToken ct) => StkCrud.Add(crud, e, ct);
    [HttpPost("update")][Permission("StkAssignmentPolicy.Update")]
    public Task<WebResponseContent> Update([FromBody] StkAssignmentPolicy e, CancellationToken ct) => StkCrud.Update(crud, e, ct);
    [HttpPost("del")][Permission("StkAssignmentPolicy.Delete")]
    public Task<WebResponseContent> Del([FromBody] JsonElement ids, CancellationToken ct) => StkCrud.Del(crud, ids, ct);

    [HttpPost("export")]
    [Permission("StkAssignmentPolicy.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

    [HttpGet("exportTemplate")]
    [Permission("StkAssignmentPolicy.Import")]
    public IActionResult ExportTemplate() =>
        EntityCrudExcel.ExportTemplate(this, crud);

    [HttpPost("import")]
    [Permission("StkAssignmentPolicy.Import")]
    [RequestSizeLimit(50_000_000)]
    public Task<WebResponseContent> Import(IFormFile file, CancellationToken ct) =>
        EntityCrudExcel.ImportAsync(crud, file, ct);

}

[Route("api/StkLocationProfile")]
[ApiController]
[Authorize]
public class StkLocationProfileController(EntityCrudService<StkLocationProfile> crud) : ControllerBase
{
    [HttpPost("getPageData")][Permission("StkLocationProfile.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions o, CancellationToken ct) => StkCrud.Page(crud, o, ct);
    [HttpPost("add")][Permission("StkLocationProfile.Add")]
    public Task<WebResponseContent> Add([FromBody] StkLocationProfile e, CancellationToken ct) => StkCrud.Add(crud, e, ct);
    [HttpPost("update")][Permission("StkLocationProfile.Update")]
    public Task<WebResponseContent> Update([FromBody] StkLocationProfile e, CancellationToken ct) => StkCrud.Update(crud, e, ct);
    [HttpPost("del")][Permission("StkLocationProfile.Delete")]
    public Task<WebResponseContent> Del([FromBody] JsonElement ids, CancellationToken ct) => StkCrud.Del(crud, ids, ct);

    [HttpPost("export")]
    [Permission("StkLocationProfile.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

    [HttpGet("exportTemplate")]
    [Permission("StkLocationProfile.Import")]
    public IActionResult ExportTemplate() =>
        EntityCrudExcel.ExportTemplate(this, crud);

    [HttpPost("import")]
    [Permission("StkLocationProfile.Import")]
    [RequestSizeLimit(50_000_000)]
    public Task<WebResponseContent> Import(IFormFile file, CancellationToken ct) =>
        EntityCrudExcel.ImportAsync(crud, file, ct);

}

[Route("api/StkRoute")]
[ApiController]
[Authorize]
public class StkRouteController(EntityCrudService<StkRoute> crud) : ControllerBase
{
    [HttpPost("getPageData")][Permission("StkRoute.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions o, CancellationToken ct) => StkCrud.Page(crud, o, ct);
    [HttpPost("add")][Permission("StkRoute.Add")]
    public Task<WebResponseContent> Add([FromBody] StkRoute e, CancellationToken ct) => StkCrud.Add(crud, e, ct);
    [HttpPost("update")][Permission("StkRoute.Update")]
    public Task<WebResponseContent> Update([FromBody] StkRoute e, CancellationToken ct) => StkCrud.Update(crud, e, ct);
    [HttpPost("del")][Permission("StkRoute.Delete")]
    public Task<WebResponseContent> Del([FromBody] JsonElement ids, CancellationToken ct) => StkCrud.Del(crud, ids, ct);

    [HttpPost("export")]
    [Permission("StkRoute.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

    [HttpGet("exportTemplate")]
    [Permission("StkRoute.Import")]
    public IActionResult ExportTemplate() =>
        EntityCrudExcel.ExportTemplate(this, crud);

    [HttpPost("import")]
    [Permission("StkRoute.Import")]
    [RequestSizeLimit(50_000_000)]
    public Task<WebResponseContent> Import(IFormFile file, CancellationToken ct) =>
        EntityCrudExcel.ImportAsync(crud, file, ct);

}

[Route("api/StkDeviceCoder")]
[ApiController]
[Authorize]
public class StkDeviceCoderController(EntityCrudService<StkDeviceCoder> crud) : ControllerBase
{
    [HttpPost("getPageData")][Permission("StkDeviceCoder.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions o, CancellationToken ct) => StkCrud.Page(crud, o, ct);
    [HttpPost("add")][Permission("StkDeviceCoder.Add")]
    public Task<WebResponseContent> Add([FromBody] StkDeviceCoder e, CancellationToken ct) => StkCrud.Add(crud, e, ct);
    [HttpPost("update")][Permission("StkDeviceCoder.Update")]
    public Task<WebResponseContent> Update([FromBody] StkDeviceCoder e, CancellationToken ct) => StkCrud.Update(crud, e, ct);
    [HttpPost("del")][Permission("StkDeviceCoder.Delete")]
    public Task<WebResponseContent> Del([FromBody] JsonElement ids, CancellationToken ct) => StkCrud.Del(crud, ids, ct);

    [HttpPost("export")]
    [Permission("StkDeviceCoder.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

    [HttpGet("exportTemplate")]
    [Permission("StkDeviceCoder.Import")]
    public IActionResult ExportTemplate() =>
        EntityCrudExcel.ExportTemplate(this, crud);

    [HttpPost("import")]
    [Permission("StkDeviceCoder.Import")]
    [RequestSizeLimit(50_000_000)]
    public Task<WebResponseContent> Import(IFormFile file, CancellationToken ct) =>
        EntityCrudExcel.ImportAsync(crud, file, ct);

}

[Route("api/StkPutAwayTask")]
[ApiController]
[Authorize]
public class StkPutAwayTaskController(EntityCrudService<StkPutAwayTask> crud) : ControllerBase
{
    [HttpPost("getPageData")][Permission("StkPutAwayTask.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions o, CancellationToken ct) => StkCrud.Page(crud, o, ct);
    [HttpPost("update")][Permission("StkPutAwayTask.Update")]
    public Task<WebResponseContent> Update([FromBody] StkPutAwayTask e, CancellationToken ct) => StkCrud.Update(crud, e, ct);

    [HttpPost("export")]
    [Permission("StkPutAwayTask.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

}

[Route("api/StkRetrievalTask")]
[ApiController]
[Authorize]
public class StkRetrievalTaskController(EntityCrudService<StkRetrievalTask> crud) : ControllerBase
{
    [HttpPost("getPageData")][Permission("StkRetrievalTask.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions o, CancellationToken ct) => StkCrud.Page(crud, o, ct);
    [HttpPost("update")][Permission("StkRetrievalTask.Update")]
    public Task<WebResponseContent> Update([FromBody] StkRetrievalTask e, CancellationToken ct) => StkCrud.Update(crud, e, ct);

    [HttpPost("export")]
    [Permission("StkRetrievalTask.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

}

[Route("api/StkDeviceTask")]
[ApiController]
[Authorize]
public class StkDeviceTaskController(EntityCrudService<StkDeviceTask> crud) : ControllerBase
{
    [HttpPost("getPageData")][Permission("StkDeviceTask.Search")]
    public Task<WebResponseContent> GetPageData([FromBody] PageDataOptions o, CancellationToken ct) => StkCrud.Page(crud, o, ct);
    [HttpPost("update")][Permission("StkDeviceTask.Update")]
    public Task<WebResponseContent> Update([FromBody] StkDeviceTask e, CancellationToken ct) => StkCrud.Update(crud, e, ct);

    [HttpPost("export")]
    [Permission("StkDeviceTask.Export")]
    public Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken ct) =>
        EntityCrudExcel.ExportAsync(this, crud, options, ct);

}
