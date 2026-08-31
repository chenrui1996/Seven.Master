using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Seven.Application.Pda;
using Seven.Application.Wms;
using Seven.Infrastructure.Pda;
using Seven.Infrastructure.Wcs.Bus;

namespace Seven.Infrastructure.Wms;

/// <summary>WMS 主数据与账本服务注册。始终注册；Features 仅控制前端菜单显示。</summary>
public static class WmsServiceCollectionExtensions
{
    public static IServiceCollection AddSevenWms(this IServiceCollection services, IConfiguration configuration)
    {
        _ = configuration;
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IContainerService, ContainerService>();
        services.AddScoped<IInboundOrderService, InboundOrderService>();
        services.AddScoped<IPickingService, PickingService>();
        services.AddScoped<IOutboundOrderService, OutboundOrderService>();
        services.AddScoped<ICycleCountService, CycleCountService>();
        services.AddScoped<ITransportOrderRequest, BusTransportOrderRequest>();
        services.AddScoped<IPdaService, PdaService>();
        return services;
    }
}
