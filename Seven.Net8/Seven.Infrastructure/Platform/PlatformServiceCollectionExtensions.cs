using Microsoft.Extensions.DependencyInjection;
using Seven.Application.Platform;

namespace Seven.Infrastructure.Platform;

/// <summary>平台基础设施 DI（接口日志、联锁）。始终注册，与 Features 无关。</summary>
public static class PlatformServiceCollectionExtensions
{
    public static IServiceCollection AddSevenPlatform(this IServiceCollection services)
    {
        services.AddScoped<IInterfaceLogService, InterfaceLogService>();
        services.AddScoped<IControlModeService, ControlModeService>();
        return services;
    }
}
