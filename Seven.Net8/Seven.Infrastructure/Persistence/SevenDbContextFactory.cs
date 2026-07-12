using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Pomelo.EntityFrameworkCore.MySql;
using Seven.Domain.Enums;
using Seven.Infrastructure.Configuration;

namespace Seven.Infrastructure.Persistence;

/// <summary>
/// EF Core 设计时 DbContext 工厂（dotnet ef migrations 使用）
/// </summary>
public class SevenDbContextFactory : IDesignTimeDbContextFactory<SevenDbContext>
{
    /// <inheritdoc />
    public SevenDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var configPath = ResolveWebApiConfigPath();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(configPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var dbOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
            ?? new DatabaseOptions();

        var builder = new DbContextOptionsBuilder<SevenDbContext>();
        ConfigureProvider(builder, dbOptions);
        return new SevenDbContext(builder.Options);
    }

    internal static void ConfigureProvider(DbContextOptionsBuilder builder, DatabaseOptions dbOptions)
    {
        var provider = Enum.TryParse<DatabaseProvider>(dbOptions.Provider, true, out var p)
            ? p
            : DatabaseProvider.MySql;

        switch (provider)
        {
            case DatabaseProvider.SqlServer:
                builder.UseSqlServer(dbOptions.ConnectionString);
                break;
            case DatabaseProvider.PgSql:
                builder.UseNpgsql(dbOptions.ConnectionString);
                break;
            default:
                builder.UseMySql(dbOptions.ConnectionString, new MySqlServerVersion(new Version(8, 0, 36)));
                break;
        }
    }

    static string ResolveWebApiConfigPath()
    {
        var candidates = new[]
        {
            Directory.GetCurrentDirectory(),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "Seven.WebApi"),
            Path.Combine(Directory.GetCurrentDirectory(), "Seven.WebApi"),
        };

        foreach (var candidate in candidates)
        {
            var full = Path.GetFullPath(candidate);
            if (File.Exists(Path.Combine(full, "appsettings.json")))
                return full;
        }

        throw new InvalidOperationException("未找到 Seven.WebApi/appsettings.json，无法执行 dotnet ef 命令。");
    }
}
