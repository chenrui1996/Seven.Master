using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Seven.Application.Interfaces;
using Seven.Application.Platform;
using Seven.Application.Wcs;
using Seven.Application.Wms;
using Seven.Infrastructure.Configuration;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Wcs.Bus;
using Seven.Infrastructure.Wcs.External;
using Seven.Infrastructure.Wcs.Packs.FourWay;
using Seven.Infrastructure.Wcs.Packs.Stacker;
using Seven.Infrastructure.Wcs.Triggers;
using Seven.Infrastructure.Wms;

namespace Seven.Infrastructure.Wcs;

/// <summary>WCS/总线/包 DI。始终注册；Features 仅控制前端菜单显示。</summary>
public static class WcsServiceCollectionExtensions
{
    public static IServiceCollection AddSevenWcs(this IServiceCollection services, IConfiguration configuration)
    {
        var externalEntries = configuration.GetSection(ExternalWcsEntryOptions.SectionName)
            .Get<List<ExternalWcsEntryOptions>>() ?? [];

        services.AddSingleton<IEquipmentTriggerPort, InMemoryEquipmentTriggerPort>();
        services.AddScoped<IBusTransportOrderQuery, BusTransportOrderQuery>();

        services.AddHttpClient(ExternalWcsHttpClient.Name);
        services.AddSingleton<MqExternalTransport>();
        services.AddSingleton<ExternalTransportFactory>();
        services.AddSingleton<IVendorCodec, FakeVendorCodec>();
        services.AddSingleton<VendorCodecRegistry>();
        services.AddScoped<ExternalWcsPackFactory>();

        foreach (var entry in externalEntries.Where(x => x.Enabled))
        {
            var captured = entry;
            services.AddScoped<IWcsPack>(sp =>
                sp.GetRequiredService<ExternalWcsPackFactory>().Create(captured));
        }

        services.AddScoped<IWcsPackResolver, WcsPackResolver>();
        services.AddScoped<OrchestrationBus>();
        services.AddScoped<IOrchestrationBus>(sp => sp.GetRequiredService<OrchestrationBus>());
        services.AddScoped<IWmsTransportCompletionHandler, WmsTransportCompletionHandler>();
        services.AddHostedService<OrchestrationBusHostedService>();

        services.AddScoped<StackerAisleAllocator>();
        services.AddScoped<StackerLocationAllocator>();
        services.AddScoped<StackerPathDispatcher>();
        services.AddScoped(sp => new StackerDepthGuard(
            sp.GetRequiredService<SevenDbContext>(),
            sp));
        services.AddScoped<StackerLocationSchema>();
        services.AddScoped<IWcsLocationSchema>(sp => sp.GetRequiredService<StackerLocationSchema>());
        services.AddScoped<StackerInboundAllocator>();
        services.AddScoped<IWcsLocationAllocator>(sp => sp.GetRequiredService<StackerInboundAllocator>());
        services.AddScoped<StackerWcsPack>(sp => new StackerWcsPack(
            sp.GetRequiredService<SevenDbContext>(),
            sp.GetRequiredService<IControlModeService>(),
            sp.GetRequiredService<IEquipmentTriggerPort>(),
            sp.GetRequiredService<StackerPathDispatcher>(),
            sp.GetRequiredService<StackerDepthGuard>()));
        services.AddScoped<IWcsPack>(sp => sp.GetRequiredService<StackerWcsPack>());
        services.AddScoped(sp => new StackerDestinationService(
            sp.GetRequiredService<SevenDbContext>(),
            sp.GetRequiredService<IEquipmentTriggerPort>(),
            sp.GetRequiredService<StackerAisleAllocator>(),
            sp.GetRequiredService<StackerLocationAllocator>(),
            sp.GetRequiredService<StackerPathDispatcher>(),
            sp.GetService<IOrchestrationBus>(),
            sp.GetRequiredService<StackerWcsPack>()));
        services.AddHostedService<StackerSchedulerHostedService>();

        services.AddSingleton(sp => new FourWayTrafficGuard(sp.GetService<IHotStore>()));
        services.AddScoped<FourWayPathDispatcher>();
        services.AddScoped<FourWayHoistOrchestrator>();
        services.AddScoped<FourWayLocationSchema>();
        services.AddScoped<IWcsLocationSchema>(sp => sp.GetRequiredService<FourWayLocationSchema>());
        services.AddScoped<FourWayInboundAllocator>();
        services.AddScoped<IWcsLocationAllocator>(sp => sp.GetRequiredService<FourWayInboundAllocator>());
        services.AddScoped<FourWayWcsPack>(sp => new FourWayWcsPack(
            sp.GetRequiredService<SevenDbContext>(),
            sp.GetRequiredService<IControlModeService>(),
            sp.GetRequiredService<IEquipmentTriggerPort>(),
            sp.GetRequiredService<FourWayPathDispatcher>(),
            sp.GetRequiredService<FourWayHoistOrchestrator>()));
        services.AddScoped<IWcsPack>(sp => sp.GetRequiredService<FourWayWcsPack>());
        services.AddScoped(sp => new FourWayDestinationService(
            sp.GetRequiredService<SevenDbContext>(),
            sp.GetRequiredService<IEquipmentTriggerPort>(),
            sp.GetRequiredService<FourWayInboundAllocator>(),
            sp.GetRequiredService<FourWayPathDispatcher>(),
            sp.GetService<IOrchestrationBus>(),
            sp.GetRequiredService<FourWayWcsPack>(),
            sp.GetRequiredService<FourWayHoistOrchestrator>()));
        services.AddHostedService<FourWaySchedulerHostedService>();

        services.AddScoped<IWcsLocationAllocatorResolver, WcsLocationAllocatorResolver>();

        return services;
    }
}
