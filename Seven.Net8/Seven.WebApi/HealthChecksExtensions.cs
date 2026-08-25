using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using Seven.Domain.Enums;
using Seven.Infrastructure.Configuration;
using Seven.Infrastructure.Persistence;

namespace Seven.WebApi;

public static class HealthChecksExtensions
{
    public static IServiceCollection AddSevenHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var db = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
            ?? new DatabaseOptions();
        var cache = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>()
            ?? new CacheOptions();
        var mq = configuration.GetSection(MessageQueueOptions.SectionName).Get<MessageQueueOptions>()
            ?? new MessageQueueOptions();
        var minio = configuration.GetSection(MinioOptions.SectionName).Get<MinioOptions>()
            ?? new MinioOptions();

        var hc = services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy());

        if (environment.IsEnvironment("Testing"))
        {
            // 集成测试用 InMemory DbContext，社区库检查会打真实连接串
            hc.AddDbContextCheck<SevenDbContext>("database");
        }
        else
        {
            var provider = Enum.TryParse<DatabaseProvider>(db.Provider, true, out var p)
                ? p
                : DatabaseProvider.MySql;
            switch (provider)
            {
                case DatabaseProvider.SqlServer:
                    hc.AddSqlServer(db.ConnectionString, name: "database");
                    break;
                case DatabaseProvider.PgSql:
                    hc.AddNpgSql(db.ConnectionString, name: "database");
                    break;
                default:
                    hc.AddMySql(db.ConnectionString, name: "database");
                    break;
            }
        }

        if (Enum.TryParse<CacheProvider>(cache.Provider, true, out var cacheProvider)
            && cacheProvider == CacheProvider.Redis
            && !string.IsNullOrWhiteSpace(cache.RedisConnectionString))
        {
            hc.AddRedis(cache.RedisConnectionString, name: "redis");
        }

        if (Enum.TryParse<MessageQueueProvider>(mq.Provider, true, out var mqProvider)
            && mqProvider == MessageQueueProvider.RabbitMQ)
        {
            var uri = BuildAmqpUri(mq.RabbitMq);
            services.AddSingleton<IConnection>(_ =>
            {
                var factory = new ConnectionFactory
                {
                    Uri = uri,
                    AutomaticRecoveryEnabled = true,
                };
                return factory.CreateConnectionAsync().GetAwaiter().GetResult();
            });
            hc.AddRabbitMQ(name: "rabbitmq");
        }

        if (minio.Enabled)
        {
            services.AddSingleton<MinioHealthCheck>();
            hc.AddCheck<MinioHealthCheck>("minio");
        }

        services
            .AddHealthChecksUI(setup =>
            {
                setup.SetEvaluationTimeInSeconds(15);
                setup.MaximumHistoryEntriesPerEndpoint(60);
                setup.AddHealthCheckEndpoint("seven-api", "/health");
            })
            .AddInMemoryStorage();

        return services;
    }

    public static WebApplication MapSevenHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        });
        app.MapHealthChecksUI(options =>
        {
            options.UIPath = "/health-ui";
        });
        return app;
    }

    private static Uri BuildAmqpUri(RabbitMqOptions opts)
    {
        var user = Uri.EscapeDataString(opts.Username);
        var pass = Uri.EscapeDataString(opts.Password);
        var vhost = string.IsNullOrEmpty(opts.VirtualHost) ? "/" : opts.VirtualHost;
        // AMQP URI 中 vhost "/" 需编码为 %2F
        var vhostSegment = Uri.EscapeDataString(vhost);
        return new Uri($"amqp://{user}:{pass}@{opts.Host}:{opts.Port}/{vhostSegment}");
    }
}
