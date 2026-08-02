using Microsoft.AspNetCore.Mvc;
using Seven.Application.Interfaces;
using Seven.Domain.Common;
using Seven.Domain.Entities.Business;
using Seven.Infrastructure.Security;

namespace Seven.WebApi.Controllers.Generated;

/// <summary>SubDevice API（Device 子表 Demo）</summary>
[Route("api/SubDevice")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
public class SubDeviceController : ControllerBase
{
    private readonly ISysSubDeviceService _service;

    public SubDeviceController(ISysSubDeviceService service) => _service = service;

    [HttpPost("getPageData")]
    [Permission("SubDevice.Search")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken cancellationToken) =>
        WebResponseContent.Ok(data: await _service.GetPageDataAsync(options, cancellationToken));

    [HttpPost("add")]
    [Permission("SubDevice.Add")]
    public Task<WebResponseContent> Add([FromBody] SubDevice entity, CancellationToken cancellationToken) =>
        _service.AddAsync(entity, cancellationToken);

    [HttpPost("update")]
    [Permission("SubDevice.Update")]
    public Task<WebResponseContent> Update([FromBody] SubDevice entity, CancellationToken cancellationToken) =>
        _service.UpdateAsync(entity, cancellationToken);

    [HttpPost("del")]
    [Permission("SubDevice.Delete")]
    public Task<WebResponseContent> Delete([FromBody] int[] ids, CancellationToken cancellationToken) =>
        _service.DeleteAsync(ids, cancellationToken);

    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken cancellationToken)
    {
        var bytes = await _service.ExportAsync(options, cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SubDevice.xlsx");
    }

    [HttpGet("exportTemplate")]
    public IActionResult ExportTemplate()
    {
        var bytes = _service.ExportTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SubDevice_template.xlsx");
    }

    [HttpPost("import")]
    public async Task<WebResponseContent> Import(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return WebResponseContent.Error("请上传 Excel 文件");
        await using var stream = file.OpenReadStream();
        return await _service.ImportAsync(stream, cancellationToken);
    }
}
