using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Seven.Application.Simulator;

namespace Seven.Infrastructure.Simulator;

/// <summary>仿真 Deploy 服务。始终注册；Features.Simulator 仅控制前端入口。</summary>
public static class SimulatorServiceCollectionExtensions
{
    public static IServiceCollection AddSevenSimulator(this IServiceCollection services, IConfiguration configuration)
    {
        _ = configuration;
        services.AddScoped<ISimulationDeployService, SimulationDeployService>();
        return services;
    }
}
