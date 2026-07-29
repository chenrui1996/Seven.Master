using Microsoft.AspNetCore.Mvc;
using Seven.Application.Interfaces;
using Seven.Domain.Common;
using Seven.Domain.Entities.Business;

namespace Seven.WebApi.Controllers.Generated;

/// <summary>Device API（代码生成）</summary>
[Route("api/Device")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
public class DeviceController : ControllerBase
{
    private readonly ISysDeviceService _service;

    public DeviceController(ISysDeviceService service) => _service = service;

    [HttpPost("getPageData")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken cancellationToken) =>
        WebResponseContent.Ok(data: await _service.GetPageDataAsync(options, cancellationToken));

    [HttpPost("add")]
    public Task<WebResponseContent> Add([FromBody] Device entity, CancellationToken cancellationToken) =>
        _service.AddAsync(entity, cancellationToken);

    [HttpPost("update")]
    public Task<WebResponseContent> Update([FromBody] Device entity, CancellationToken cancellationToken) =>
        _service.UpdateAsync(entity, cancellationToken);

    [HttpPost("del")]
    public Task<WebResponseContent> Delete([FromBody] int[] ids, CancellationToken cancellationToken) =>
        _service.DeleteAsync(ids, cancellationToken);

    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody] PageDataOptions options, CancellationToken cancellationToken)
    {
        var bytes = await _service.ExportAsync(options, cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Device.xlsx");
    }

    [HttpGet("exportTemplate")]
    public IActionResult ExportTemplate()
    {
        var bytes = _service.ExportTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Device_template.xlsx");
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
