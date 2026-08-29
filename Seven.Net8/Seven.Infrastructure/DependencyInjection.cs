using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Pomelo.EntityFrameworkCore.MySql;
using Seven.Application.Interfaces;
using Seven.Domain.Enums;
using Seven.Infrastructure.Caching;
using Seven.Infrastructure.Configuration;
using Seven.Infrastructure.DeviceComm;
using Seven.Infrastructure.HotStore;
using Seven.Infrastructure.Platform;
using Seven.Infrastructure.Scada;
using Seven.Infrastructure.Simulator;
using Seven.Infrastructure.Wcs;
using Seven.Infrastructure.Wms;
using Seven.Infrastructure.Mail;
using Seven.Infrastructure.Messaging;
using Seven.Infrastructure.Messaging.Outbox;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Quartz;
using Seven.Infrastructure.Security;
using Seven.Infrastructure.Services;
using Seven.Infrastructure.Storage;

namespace Seven.Infrastructure;

/// <summary>
/// 基础设施层依赖注入扩展
/// </summary>
public static class DependencyInjection
{
    /// <summary>注册 Seven 基础设施与应用服务</summary>
    public static IServiceCollection AddSevenInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<MinioOptions>(configuration.GetSection(MinioOptions.SectionName));
        services.Configure<CorsOptions>(configuration.GetSection(CorsOptions.SectionName));
        services.Configure<AlarmOptions>(configuration.GetSection(AlarmOptions.SectionName));
        services.Configure<MailOptions>(configuration.GetSection(MailOptions.SectionName));
        services.Configure<TenantOptions>(configuration.GetSection(TenantOptions.SectionName));
        services.Configure<SecurityOptions>(configuration.GetSection(SecurityOptions.SectionName));
        services.Configure<FeatureOptions>(configuration.GetSection(FeatureOptions.SectionName));
        services.Configure<HotStoreOptions>(configuration.GetSection(HotStoreOptions.SectionName));

        var features = configuration.GetSection(FeatureOptions.SectionName).Get<FeatureOptions>() ?? new FeatureOptions();

        services.AddHttpContextAccessor();
        services.AddHttpClient("quartz");
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<IOutboxStore, EfOutboxStore>();
        services.AddScoped<ITenantContextInitializer, TenantContextInitializer>();

        AddDatabase(services, configuration, features);
        AddCache(services, configuration);
        services.AddSevenHotStore(configuration);
        services.AddSevenDeviceComm(configuration);
        services.AddSevenPlatform();
        services.AddSevenWcs(configuration);
        services.AddSevenWms(configuration);
        services.AddScoped(typeof(Seven.Infrastructure.Crud.EntityCrudService<>));
        services.AddSevenScada();
        services.AddSevenSimulator(configuration);
        AddAuthentication(services, configuration);
        services.AddSevenMessageQueue(configuration);
        if (features.Quartz)
            services.AddSevenQuartz();
        else
            services.AddSevenQuartzStub();
        if (features.Outbox && features.MessageQueue)
            services.AddHostedService<OutboxProcessor>();
        AddApplicationServices(services);

        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICaptchaService, CaptchaService>();
        services.AddScoped<IDataScopeService, DataScopeService>();
        services.AddScoped<IEmailService, MailKitEmailService>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        var minio = configuration.GetSection(MinioOptions.SectionName).Get<MinioOptions>() ?? new MinioOptions();
        if (features.MinIO && minio.Enabled)
            services.AddScoped<IFileStorageService, MinioFileStorageService>();
        else
            services.AddScoped<IFileStorageService, LocalFileStorageService>();

        var security = configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>() ?? new SecurityOptions();
        // 始终注册限流（供 [EnableRateLimiting]）；Features.RateLimit=false 时额度极大，等同关闭
        var loginLimit = features.RateLimit ? Math.Max(5, security.LoginPermitLimit) : int.MaxValue;
        var globalLimit = features.RateLimit ? 300 : int.MaxValue;
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("login", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = loginLimit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = globalLimit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });

        return services;
    }

    private static void AddDatabase(IServiceCollection services, IConfiguration configuration, FeatureOptions features)
    {
        var dbOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
        var auditEnabled = features.AuditInterceptor;

        services.AddDbContext<SevenDbContext>((sp, options) =>
        {
            var provider = Enum.TryParse<DatabaseProvider>(dbOptions.Provider, true, out var p) ? p : DatabaseProvider.MySql;
            switch (provider)
            {
                case DatabaseProvider.SqlServer:
                    options.UseSqlServer(dbOptions.ConnectionString);
                    break;
                case DatabaseProvider.PgSql:
                    options.UseNpgsql(dbOptions.ConnectionString);
                    break;
                default:
                    options.UseMySql(dbOptions.ConnectionString, new MySqlServerVersion(new Version(8, 0, 36)));
                    break;
            }
            if (auditEnabled)
                options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });
    }

    private static void AddCache(IServiceCollection services, IConfiguration configuration)
    {
        var cacheOptions = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>() ?? new CacheOptions();
        var useRedis = Enum.TryParse<CacheProvider>(cacheOptions.Provider, true, out var p) && p == CacheProvider.Redis;

        if (useRedis)
        {
            services.AddStackExchangeRedisCache(o => o.Configuration = cacheOptions.RedisConnectionString);
        }
        else
        {
            services.AddMemoryCache();
        }

        services.AddSingleton<ICacheService>(sp =>
        {
            ICacheService inner = useRedis
                ? new RedisCacheService(sp.GetRequiredService<IDistributedCache>())
                : new MemoryCacheService(sp.GetRequiredService<IMemoryCache>());
            return new DelayedDoubleDeleteCacheService(inner, sp.GetRequiredService<IOptions<CacheOptions>>());
        });
    }

    private static void AddAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret))
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            (path.StartsWithSegments("/hub/alarm")
                             || path.StartsWithSegments("/hub/message")
                             || path.StartsWithSegments("/hub/devicecomm")))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });
        services.AddAuthorization();
    }

    private static void AddApplicationServices(IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISysUserService, SysUserService>();
        services.AddScoped<ISysRoleService, SysRoleService>();
        services.AddScoped<ISysMenuService, SysMenuService>();
        services.AddScoped<ISysDepartmentService, SysDepartmentService>();
        services.AddScoped<ISysDictionaryService, SysDictionaryService>();
        services.AddScoped<ISysLogService, SysLogService>();
        services.AddScoped<IAlarmService, AlarmService>();
        RegisterUnregisteredSysServices(services);
    }

    private static void RegisterUnregisteredSysServices(IServiceCollection services)
    {
        var infraAssembly = typeof(SysUserService).Assembly;
        foreach (var impl in infraAssembly.GetExportedTypes()
                     .Where(t => t is { IsClass: true, IsAbstract: false }
                                 && t.Namespace == "Seven.Infrastructure.Services"
                                 && t.Name.StartsWith("Sys", StringComparison.Ordinal)
                                 && t.Name.EndsWith("Service", StringComparison.Ordinal)))
        {
            var iface = impl.GetInterfaces()
                .FirstOrDefault(i => i.Name == $"I{impl.Name}"
                                     && i.Namespace == "Seven.Application.Interfaces");
            if (iface == null) continue;
            if (services.Any(d => d.ServiceType == iface)) continue;
            services.AddScoped(iface, impl);
        }
    }
}

/// <summary>将租户 Claim 应用到 DbContext</summary>
public interface ITenantContextInitializer
{
    void Apply(SevenDbContext db);
}

public class TenantContextInitializer : ITenantContextInitializer
{
    private readonly IHttpContextAccessor _http;
    private readonly IOptions<TenantOptions> _options;
    private readonly IOptions<FeatureOptions> _features;

    public TenantContextInitializer(
        IHttpContextAccessor http,
        IOptions<TenantOptions> options,
        IOptions<FeatureOptions> features)
    {
        _http = http;
        _options = options;
        _features = features;
    }

    public void Apply(SevenDbContext db)
    {
        if (!_features.Value.Tenant || !_options.Value.Enabled) return;
        db.TenantFilterEnabled = true;
        var claim = _http.HttpContext?.User?.FindFirst("TenantId")?.Value;
        db.CurrentTenantId = int.TryParse(claim, out var tid) ? tid : 0;
    }
}
