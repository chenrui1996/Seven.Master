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

    public SimulationController(ISimulationDeployService deploy) => _deploy = deploy;

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

    [HttpGet("deployments")]
    public async Task<WebResponseContent> List(CancellationToken ct) =>
        WebResponseContent.Ok(data: await _deploy.ListDeploymentsAsync(ct));
}

public record UndeployRequest(string ProjectName, bool RemoveLocations = false);
