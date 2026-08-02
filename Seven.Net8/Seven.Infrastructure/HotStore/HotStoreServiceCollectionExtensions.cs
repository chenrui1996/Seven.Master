using System.Threading.Channels;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Seven.Application.Interfaces;
using Seven.Domain.Enums;
using Seven.Infrastructure.Configuration;
using Seven.Infrastructure.HotStore.Demo;
using StackExchange.Redis;

namespace Seven.Infrastructure.HotStore;

/// <summary>HotStore DI 注册</summary>
public static class HotStoreServiceCollectionExtensions
{
    /// <summary>按 Features.HotStore 注册热数据通道</summary>
    public static IServiceCollection AddSevenHotStore(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<HotStoreOptions>(configuration.GetSection(HotStoreOptions.SectionName));

        var features = configuration.GetSection(FeatureOptions.SectionName).Get<FeatureOptions>() ?? new FeatureOptions();
        if (!features.HotStore)
        {
            services.AddSingleton<IHotStore, DisabledHotStore>();
            return services;
        }

        var options = configuration.GetSection(HotStoreOptions.SectionName).Get<HotStoreOptions>() ?? new HotStoreOptions();
        var useRedis = Enum.TryParse<CacheProvider>(options.Provider, true, out var p) && p == CacheProvider.Redis;

        var channel = Channel.CreateUnbounded<HotStoreChange>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        services.AddSingleton(channel);
        services.AddSingleton(channel.Reader);
        services.AddSingleton(channel.Writer);

        if (useRedis)
        {
            var redisCs = string.IsNullOrWhiteSpace(options.RedisConnectionString)
                ? configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>()?.RedisConnectionString
                : options.RedisConnectionString;
            if (string.IsNullOrWhiteSpace(redisCs))
                throw new InvalidOperationException("HotStore Provider=Redis requires HotStore:RedisConnectionString or Cache:RedisConnectionString.");

            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisCs));
            services.AddSingleton<IHotStore>(sp =>
            {
                var inner = new RedisHotStore(
                    sp.GetRequiredService<IConnectionMultiplexer>(),
                    sp.GetRequiredService<IOptions<HotStoreOptions>>());
                return new TrackingHotStore(inner, sp.GetRequiredService<ChannelWriter<HotStoreChange>>());
            });
        }
        else
        {
            services.AddMemoryCache();
            services.AddSingleton<IHotStore>(sp =>
            {
                var inner = new MemoryHotStore(sp.GetRequiredService<IOptions<HotStoreOptions>>());
                return new TrackingHotStore(inner, sp.GetRequiredService<ChannelWriter<HotStoreChange>>());
            });
        }

        services.AddHostedService<HotStoreHostedService>();

        if (options.EnableDemoScheduler)
        {
            services.AddSingleton<IHotStoreWarmup, WcsMapWarmup>();
            services.AddSingleton<IHotStorePersister, WcsTrafficPersister>();
            services.AddHostedService<WcsDemoSchedulerHostedService>();
        }

        return services;
    }
}
