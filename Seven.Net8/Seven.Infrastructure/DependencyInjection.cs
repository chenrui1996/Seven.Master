using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Seven.Application.Interfaces;
using Seven.Infrastructure.Services;
using Seven.Domain.Enums;
using Seven.Infrastructure.Caching;
using Seven.Infrastructure.Configuration;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Security;
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

        AddDatabase(services, configuration);
        AddCache(services, configuration);
        AddAuthentication(services, configuration);
        AddApplicationServices(services);

        services.AddHttpContextAccessor();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        return services;
    }

    private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
    {
        var dbOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
        services.AddDbContext<SevenDbContext>(options =>
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
        services.AddScoped<IWorkFlowService, WorkFlowService>();
    }
}
