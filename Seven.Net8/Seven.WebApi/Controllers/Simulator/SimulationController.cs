using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seven.Application.Simulator;
using Seven.Domain.Common;

namespace Seven.WebApi.Controllers.Simulator;

[Route("api/simulation")]
[ApiController]
[AllowAnonymous]
public class SimulationController : ControllerBase
{
    private readonly ISimulationDeployService _deploy;
    private readonly ISimulationImportService _import;

    public SimulationController(ISimulationDeployService deploy, ISimulationImportService import)
    {
        _deploy = deploy;
        _import = import;
    }

    [HttpPost("projects/validate-features")]
    public WebResponseContent ValidateFeatures([FromBody] SimFeaturesDto features)
    {
        var result = _deploy.ValidateFeatures(features);
        return result.Ok
            ? WebResponseContent.Ok(data: result)
            : WebResponseContent.Error(string.Join("; ", result.Errors));
    }

    [HttpPost("deploy")]
    public async Task<WebResponseContent> Deploy([FromBody] SimProjectDto project, CancellationToken ct)
    {
        try
        {
            var result = await _deploy.DeployAsync(project, ct);
            return WebResponseContent.Ok(result.Message, result);
        }
        catch (Exception ex)
        {
            return WebResponseContent.Error(ex.Message);
        }
    }

    [HttpPost("undeploy")]
    public async Task<WebResponseContent> Undeploy([FromBody] UndeployRequest request, CancellationToken ct)
    {
        try
        {
            await _deploy.UndeployAsync(request.ProjectName, request.RemoveLocations, ct);
            return WebResponseContent.Ok("已 Undeploy");
        }
        catch (Exception ex)
        {
            return WebResponseContent.Error(ex.Message);
        }
    }

    [HttpPost("reset")]
    public async Task<WebResponseContent> Reset([FromBody] ResetRequest request, CancellationToken ct)
    {
        try
        {
            await _deploy.ResetAsync(request.ProjectName, ct);
            return WebResponseContent.Ok("已 Reset");
        }
        catch (Exception ex)
        {
            return WebResponseContent.Error(ex.Message);
        }
    }

    [HttpGet("deployments")]
    public async Task<WebResponseContent> List(CancellationToken ct) =>
        WebResponseContent.Ok(data: await _deploy.ListDeploymentsAsync(ct));

    [HttpPost("promote-preview")]
    public async Task<WebResponseContent> PromotePreview([FromBody] SimPromoteRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _deploy.PromotePreviewAsync(request, ct);
            return WebResponseContent.Ok(data: result);
        }
        catch (Exception ex)
        {
            return WebResponseContent.Error(ex.Message);
        }
    }

    [HttpPost("promote")]
    public async Task<WebResponseContent> Promote([FromBody] SimPromoteRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _deploy.PromoteAsync(request, ct);
            return WebResponseContent.Ok(result.Message, result);
        }
        catch (Exception ex)
        {
            return WebResponseContent.Error(ex.Message);
        }
    }

    [HttpPost("import-grid")]
    public WebResponseContent ImportGrid([FromBody] ImportGridRequest request)
    {
        try
        {
            var result = _import.ImportGrid(request);
            return WebResponseContent.Ok(data: result);
        }
        catch (Exception ex)
        {
            return WebResponseContent.Error(ex.Message);
        }
    }

    [HttpPost("route-groups/preview")]
    public WebResponseContent PreviewRouteGroups([FromBody] RouteGroupsPreviewRequest request)
    {
        try
        {
            var result = _import.PreviewRouteGroups(request);
            return WebResponseContent.Ok(data: result);
        }
        catch (Exception ex)
        {
            return WebResponseContent.Error(ex.Message);
        }
    }
}

public record UndeployRequest(string ProjectName, bool RemoveLocations = false);
public record ResetRequest(string ProjectName);
