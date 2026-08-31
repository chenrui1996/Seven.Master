using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Seven.Application.Simulator;
using Seven.Infrastructure.Simulator.Gateway;

namespace Seven.Infrastructure.Simulator;

/// <summary>仿真 Deploy 服务。始终注册；Features.Simulator 仅控制前端入口。</summary>
public static class SimulatorServiceCollectionExtensions
{
    public static IServiceCollection AddSevenSimulator(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SimGatewayOptions>(configuration.GetSection(SimGatewayOptions.SectionName));
        services.AddSingleton<ISimWcsProxyNotifier, SimWcsProxyNotifier>();
        services.AddScoped<ISimulationDeployService, SimulationDeployService>();
        services.AddScoped<ISimulationImportService, SimulationImportService>();

        var gateway = configuration.GetSection(SimGatewayOptions.SectionName).Get<SimGatewayOptions>() ?? new SimGatewayOptions();
        if (gateway.Enabled)
            services.AddHostedService<SimGatewayHostedService>();

        return services;
    }
}
