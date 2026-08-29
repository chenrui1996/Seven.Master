using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Seven.Application.Simulator;
using Seven.Domain.Common;
using Seven.Infrastructure.Security;

namespace Seven.WebApi.Controllers.Simulator;

[Route("api/simulation")]
[ApiController]
[AllowAnonymous]
[RequiresFeature("Simulator")]
public class SimulationController : ControllerBase
{
    private readonly IServiceProvider _sp;

    public SimulationController(IServiceProvider sp) => _sp = sp;

    private ISimulationDeployService DeployService =>
        _sp.GetService<ISimulationDeployService>()
        ?? throw new InvalidOperationException("Features.Simulator 未启用或未注册 ISimulationDeployService");

    [HttpPost("projects/validate-features")]
    public WebResponseContent ValidateFeatures([FromBody] SimFeaturesDto features)
    {
        var result = DeployService.ValidateFeatures(features);
        return result.Ok
            ? WebResponseContent.Ok(data: result)
            : WebResponseContent.Error(string.Join("; ", result.Errors));
    }

    [HttpPost("deploy")]
    public async Task<WebResponseContent> Deploy([FromBody] SimProjectDto project, CancellationToken ct)
    {
        try
        {
            var result = await DeployService.DeployAsync(project, ct);
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
            await DeployService.UndeployAsync(request.ProjectName, request.RemoveLocations, ct);
            return WebResponseContent.Ok("已 Undeploy");
        }
        catch (Exception ex)
        {
            return WebResponseContent.Error(ex.Message);
        }
    }

    [HttpGet("deployments")]
    public async Task<WebResponseContent> List(CancellationToken ct) =>
        WebResponseContent.Ok(data: await DeployService.ListDeploymentsAsync(ct));
}

public record UndeployRequest(string ProjectName, bool RemoveLocations = false);
