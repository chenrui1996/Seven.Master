using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seven.Domain.Common;
using Seven.Domain.Entities.Form;
using Seven.Infrastructure.Security;
using Seven.Infrastructure.Services;

namespace Seven.WebApi.Controllers;

[Route("api/FormDesignOptions")]
[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = "system")]
public class FormDesignOptionsController : ControllerBase
{
    private readonly IFormDesignService _service;

    public FormDesignOptionsController(IFormDesignService service) => _service = service;

    [HttpPost("getPageData")]
    [Permission("FormDesignOptions.Search")]
    public async Task<WebResponseContent> GetPageData([FromBody] PageDataOptions options, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _service.GetPageDataAsync(options, ct));

    [HttpGet("get/{id:int}")]
    [Permission("FormDesignOptions.Search")]
    public Task<WebResponseContent> Get(int id, CancellationToken ct) => _service.GetAsync(id, ct);

    [HttpPost("save")]
    [Permission("FormDesignOptions.Update")]
    public Task<WebResponseContent> Save([FromBody] FormDesignOptions entity, CancellationToken ct) =>
        _service.SaveAsync(entity, ct);

    [HttpPost("del")]
    [Permission("FormDesignOptions.Delete")]
    public Task<WebResponseContent> Del([FromBody] int[] ids, CancellationToken ct) =>
        _service.DeleteAsync(ids, ct);

    [HttpPost("getCollections")]
    [Permission("FormDesignOptions.Search")]
    public async Task<WebResponseContent> GetCollections([FromBody] PageDataOptions options, CancellationToken ct) =>
        WebResponseContent.Ok(data: await _service.GetCollectionsAsync(options, ct));

    /// <summary>公开填报（登录用户）</summary>
    [HttpPost("submit")]
    public Task<WebResponseContent> Submit([FromBody] FormSubmitRequest request, CancellationToken ct) =>
        _service.SubmitCollectionAsync(request.FormId, request.FormData ?? "{}", ct);
}

public class FormSubmitRequest
{
    public int FormId { get; set; }
    public string? FormData { get; set; }
}
