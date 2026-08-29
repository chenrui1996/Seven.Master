using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Seven.Application.Wms;
using Seven.Infrastructure.Configuration;
using Seven.Infrastructure.Wcs.Bus;

namespace Seven.Infrastructure.Wms;

/// <summary>WMS 主数据与账本服务注册。始终注册实现；API 用 Features.Wms 门控。</summary>
public static class WmsServiceCollectionExtensions
{
    public static IServiceCollection AddSevenWms(this IServiceCollection services, IConfiguration configuration)
    {
        var features = configuration.GetSection(FeatureOptions.SectionName).Get<FeatureOptions>() ?? new FeatureOptions();
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IContainerService, ContainerService>();
        services.AddScoped<IInboundOrderService, InboundOrderService>();
        services.AddScoped<IOutboundOrderService, OutboundOrderService>();
        services.AddScoped<ICycleCountService, CycleCountService>();
        if (features.OrchestrationBus)
            services.AddScoped<ITransportOrderRequest, BusTransportOrderRequest>();
        else
            services.AddScoped<ITransportOrderRequest, NoOpTransportOrderRequest>();
        return services;
    }
}
