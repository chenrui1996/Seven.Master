using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Seven.Application.Interfaces;
using Seven.Infrastructure.Configuration;
using Seven.Infrastructure.DeviceComm.Drivers;

namespace Seven.Infrastructure.DeviceComm;

/// <summary>DeviceComm DI 注册</summary>
public static class DeviceCommServiceCollectionExtensions
{
    public static IServiceCollection AddSevenDeviceComm(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DeviceCommOptions>(configuration.GetSection(DeviceCommOptions.SectionName));

        var features = configuration.GetSection(FeatureOptions.SectionName).Get<FeatureOptions>() ?? new FeatureOptions();
        if (!features.DeviceComm)
        {
            services.AddSingleton<IDeviceCommGateway, DisabledDeviceCommGateway>();
            services.AddSingleton<ICommRuleEngine, DisabledCommRuleEngine>();
            services.AddScoped<ICommConnectionService, CommConnectionService>();
            services.AddScoped<ICommPointService, CommPointService>();
            services.AddScoped<ICommRuleService, CommRuleService>();
            return services;
        }

        services.AddSingleton<IPlcDriverFactory, PlcDriverFactory>();
        services.AddSingleton<DeviceCommGateway>();
        services.AddSingleton<IDeviceCommGateway>(sp => sp.GetRequiredService<DeviceCommGateway>());
        services.AddSingleton<CommRuleEngine>();
        services.AddSingleton<ICommRuleEngine>(sp => sp.GetRequiredService<CommRuleEngine>());
        services.AddHostedService<DeviceCommHostedService>();

        services.AddScoped<ICommConnectionService, CommConnectionService>();
        services.AddScoped<ICommPointService, CommPointService>();
        services.AddScoped<ICommRuleService, CommRuleService>();
        return services;
    }
}
