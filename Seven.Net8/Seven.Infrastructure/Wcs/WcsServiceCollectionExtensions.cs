using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Seven.Application.Interfaces;
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

/// <summary>WCS/WMS DI 注册。OrchestrationBus / Stacker / FourWay 按 Features 门控。</summary>
public static class WcsServiceCollectionExtensions
{
    public static IServiceCollection AddSevenWcs(this IServiceCollection services, IConfiguration configuration)
    {
        var features = configuration.GetSection(FeatureOptions.SectionName).Get<FeatureOptions>() ?? new FeatureOptions();
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

        var registerExternal = features.OrchestrationBus || externalEntries.Any(x => x.Enabled);
        if (registerExternal)
        {
            foreach (var entry in externalEntries.Where(x => x.Enabled))
            {
                var captured = entry;
                services.AddScoped<IWcsPack>(sp =>
                    sp.GetRequiredService<ExternalWcsPackFactory>().Create(captured));
            }
        }

        if (features.OrchestrationBus)
        {
            services.AddScoped<IWcsPackResolver, WcsPackResolver>();
            services.AddScoped<OrchestrationBus>();
            services.AddScoped<IOrchestrationBus>(sp => sp.GetRequiredService<OrchestrationBus>());
            if (features.Wms)
                services.AddScoped<IWmsTransportCompletionHandler, WmsTransportCompletionHandler>();
            else
                services.AddScoped<IWmsTransportCompletionHandler, NoOpWmsTransportCompletionHandler>();
            services.AddHostedService<OrchestrationBusHostedService>();
        }

        if (features.WcsPacks.Stacker)
        {
            services.AddScoped<StackerAisleAllocator>();
            services.AddScoped<StackerLocationAllocator>();
            services.AddScoped<StackerWcsPack>();
            services.AddScoped<IWcsPack>(sp => sp.GetRequiredService<StackerWcsPack>());
            services.AddScoped(sp => new StackerDestinationService(
                sp.GetRequiredService<SevenDbContext>(),
                sp.GetRequiredService<IEquipmentTriggerPort>(),
                sp.GetRequiredService<StackerAisleAllocator>(),
                sp.GetRequiredService<StackerLocationAllocator>(),
                sp.GetService<IOrchestrationBus>()));
            services.AddHostedService<StackerSchedulerHostedService>();
        }

        if (features.WcsPacks.FourWay)
        {
            services.AddSingleton(sp => new FourWayTrafficGuard(sp.GetService<IHotStore>()));
            services.AddScoped<FourWayWcsPack>();
            services.AddScoped<IWcsPack>(sp => sp.GetRequiredService<FourWayWcsPack>());
            services.AddHostedService<FourWaySchedulerHostedService>();
        }

        return services;
    }
}
