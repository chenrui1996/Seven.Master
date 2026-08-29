using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Seven.Application.Simulator;
using Seven.Infrastructure.Configuration;

namespace Seven.Infrastructure.Simulator;

public static class SimulatorServiceCollectionExtensions
{
    public static IServiceCollection AddSevenSimulator(this IServiceCollection services, IConfiguration configuration)
    {
        var features = configuration.GetSection(FeatureOptions.SectionName).Get<FeatureOptions>() ?? new();
        if (!features.Simulator)
            return services;

        services.AddScoped<ISimulationDeployService, SimulationDeployService>();
        return services;
    }
}
