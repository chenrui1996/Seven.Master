using Microsoft.Extensions.DependencyInjection;
using Seven.Application.Scada;

namespace Seven.Infrastructure.Scada;

/// <summary>2D SCADA 服务注册。始终注册实现；API 用 Features.Wms 门控。</summary>
public static class ScadaServiceCollectionExtensions
{
    public static IServiceCollection AddSevenScada(this IServiceCollection services)
    {
        services.AddScoped<IScadaViewService, ScadaViewService>();
        services.AddScoped<IScadaNodeBindService, ScadaNodeBindService>();
        return services;
    }
}
