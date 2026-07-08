using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Seven.Infrastructure.Persistence;

namespace Seven.Tests;

/// <summary>
/// 集成测试 Web 应用工厂，使用 InMemory 数据库隔离测试环境。
/// </summary>
public class SevenWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _databaseName = $"SevenTest_{Guid.NewGuid():N}";

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var descriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<SevenDbContext>)
                         || d.ServiceType == typeof(SevenDbContext))
                .ToList();
            foreach (var descriptor in descriptors)
                services.Remove(descriptor);

            services.AddDbContext<SevenDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await DbSeeder.SeedAsync(Services);
    }

    /// <inheritdoc />
    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
}
