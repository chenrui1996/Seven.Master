# HealthChecks Xabaril 改造 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 用 Xabaril `AspNetCore.HealthChecks.*` 替换 `Seven.WebApi` 自写健康检查，条件注册依赖检查，并挂上 `/health-ui` 仪表盘。

**Architecture:** 新增 `HealthChecksExtensions`：读现有 Options 条件注册 `self` / `database` / `redis` / `rabbitmq` / `minio`；`MapHealthChecks("/health")` 使用 `UIResponseWriter`；`MapHealthChecksUI` 路径 `/health-ui`，InMemory 存储。删除 `SevenInfrastructureHealthCheck` 与自定义 Redis/RabbitMQ 检查。

**Tech Stack:** .NET 8、AspNetCore.HealthChecks.* 9.0.0、HealthChecks.UI + InMemory、现有 Minio SDK

## Global Constraints

- 规格：`docs/superpowers/specs/2026-08-25-healthchecks-xabaril-design.md`
- 社区包版本统一 **9.0.0**
- 未启用的 Redis / RabbitMQ / MinIO **不注册**检查项
- K8s 探针路径保持 **`/health`**；`/api/Health` 不动
- MinIO 只读 `BucketExists`，**禁止** `MakeBucket`
- 提交：仅在用户明确要求时 `git commit`（本仓库用户规则优先于计划默认「frequent commits」）
- Working directory：`Seven.Master`（仓库根）

---

## File Structure

| 文件 | 职责 |
|------|------|
| `Seven.Net8/Seven.WebApi/Seven.WebApi.csproj` | 增加 HealthChecks NuGet |
| `Seven.Net8/Seven.WebApi/HealthChecksExtensions.cs` | `AddSevenHealthChecks` / `MapSevenHealthChecks` |
| `Seven.Net8/Seven.WebApi/Program.cs` | 调用扩展，去掉旧注册 |
| `Seven.Net8/Seven.WebApi/OptionalInfrastructureHealthChecks.cs` | 仅保留只读 `MinioHealthCheck` |
| `Seven.Net8/Seven.WebApi/SevenInfrastructureHealthCheck.cs` | 删除 |
| `Seven.Net8/Seven.Tests/Integration/HealthApiTests.cs` | 断言 `/health` 与 `/health-ui` |
| `doc/02-后端开发指南.md` | 同步检查名与端点 |

---

### Task 1: 安装 NuGet 包

**Files:**
- Modify: `Seven.Net8/Seven.WebApi/Seven.WebApi.csproj`

**Interfaces:**
- Consumes: 无
- Produces: 项目可引用 `HealthChecks.*` / `HealthChecks.UI.*` 扩展方法

- [ ] **Step 1: 添加包引用**

在 `Seven.Net8/Seven.WebApi` 目录执行：

```powershell
dotnet add package AspNetCore.HealthChecks.MySql --version 9.0.0
dotnet add package AspNetCore.HealthChecks.SqlServer --version 9.0.0
dotnet add package AspNetCore.HealthChecks.NpgSql --version 9.0.0
dotnet add package AspNetCore.HealthChecks.Redis --version 9.0.0
dotnet add package AspNetCore.HealthChecks.Rabbitmq --version 9.0.0
dotnet add package AspNetCore.HealthChecks.UI --version 9.0.0
dotnet add package AspNetCore.HealthChecks.UI.Client --version 9.0.0
dotnet add package AspNetCore.HealthChecks.UI.InMemory.Storage --version 9.0.0
dotnet add package Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore --version 8.0.28
```

说明：`EntityFrameworkCore` 健康检查包仅用于 **Testing** 环境（InMemory `DbContext`），避免社区 `AddSqlServer` 连真实 LocalDB 导致集成测试失败。生产/开发仍按 Provider 用社区包。

- [ ] **Step 2: 确认 csproj 含上述 PackageReference**

Expected: `Seven.WebApi.csproj` 中可见 9 个新包；`dotnet restore Seven.Net8/Seven.WebApi/Seven.WebApi.csproj` 成功。

---

### Task 2: 精简 MinIO 检查并删除旧检查类

**Files:**
- Modify: `Seven.Net8/Seven.WebApi/OptionalInfrastructureHealthChecks.cs`
- Delete: `Seven.Net8/Seven.WebApi/SevenInfrastructureHealthCheck.cs`

**Interfaces:**
- Consumes: `MinioOptions`（`Seven.Infrastructure.Configuration`）
- Produces: `public class MinioHealthCheck : IHealthCheck`（只读）

- [ ] **Step 1: 重写 `OptionalInfrastructureHealthChecks.cs` 为仅 MinIO**

用下列完整文件内容替换（删除 `RedisHealthCheck`、`RabbitMqHealthCheck`）：

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Seven.Infrastructure.Configuration;

namespace Seven.WebApi;

/// <summary>MinIO：Enabled 时只读探测 BucketExists，不建桶</summary>
public class MinioHealthCheck : IHealthCheck
{
    private readonly IOptions<MinioOptions> _options;

    public MinioHealthCheck(IOptions<MinioOptions> options) => _options = options;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var opts = _options.Value;
        try
        {
            var client = new MinioClient()
                .WithEndpoint(opts.Endpoint)
                .WithCredentials(opts.AccessKey, opts.SecretKey)
                .WithSSL(opts.UseSsl)
                .Build();

            var exists = await client.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(opts.Bucket),
                cancellationToken);

            if (!exists)
                return HealthCheckResult.Degraded($"minio reachable, bucket `{opts.Bucket}` missing");

            return HealthCheckResult.Healthy($"minio bucket `{opts.Bucket}` ok");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"minio unreachable: {ex.Message}");
        }
    }
}
```

注意：本类仅在 `MinIO.Enabled == true` 时由扩展注册；类内不再做 `Enabled` 跳过分支。

- [ ] **Step 2: 删除 `SevenInfrastructureHealthCheck.cs`**

删除文件：`Seven.Net8/Seven.WebApi/SevenInfrastructureHealthCheck.cs`

- [ ] **Step 3: 编译确认旧类型引用会失败（下一步修 Program）**

```powershell
dotnet build Seven.Net8/Seven.WebApi/Seven.WebApi.csproj --no-restore
```

Expected: 因 `Program.cs` 仍引用已删类型而编译失败（或 restore 后再 build）。若尚未 restore，先 `dotnet restore` 再 build。

---

### Task 3: 实现 `HealthChecksExtensions`

**Files:**
- Create: `Seven.Net8/Seven.WebApi/HealthChecksExtensions.cs`

**Interfaces:**
- Consumes: `IConfiguration`、`IHostEnvironment`、`DatabaseOptions` / `CacheOptions` / `MessageQueueOptions` / `MinioOptions`、`DatabaseProvider` / `CacheProvider` / `MessageQueueProvider`
- Produces:
  - `public static IServiceCollection AddSevenHealthChecks(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)`
  - `public static WebApplication MapSevenHealthChecks(this WebApplication app)`

- [ ] **Step 1: 新建扩展文件**

创建 `Seven.Net8/Seven.WebApi/HealthChecksExtensions.cs`：

```csharp
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
```

- [ ] **Step 2: 编译扩展是否可解析**

```powershell
dotnet build Seven.Net8/Seven.WebApi/Seven.WebApi.csproj
```

Expected: 若 `Program.cs` 仍用旧 API 可能继续失败；扩展文件本身应无语法/引用错误。若 `AddMySql` / `AddRedis` 签名与 9.0 不完全一致，按编译器提示改为命名参数形式（保持 `name: "database"` 等检查名不变）。

---

### Task 4: 接线 `Program.cs`

**Files:**
- Modify: `Seven.Net8/Seven.WebApi/Program.cs`

**Interfaces:**
- Consumes: `AddSevenHealthChecks`、`MapSevenHealthChecks`
- Produces: 启动时注册社区健康检查与 UI 端点

- [ ] **Step 1: 替换服务注册**

删除：

```csharp
builder.Services.AddSingleton<SevenInfrastructureHealthCheck>();
builder.Services.AddSingleton<RedisHealthCheck>();
builder.Services.AddSingleton<RabbitMqHealthCheck>();
builder.Services.AddSingleton<MinioHealthCheck>();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddCheck<SevenInfrastructureHealthCheck>("infrastructure")
    .AddCheck<RedisHealthCheck>("redis")
    .AddCheck<RabbitMqHealthCheck>("rabbitmq")
    .AddCheck<MinioHealthCheck>("minio");
```

改为：

```csharp
builder.Services.AddSevenHealthChecks(builder.Configuration, builder.Environment);
```

- [ ] **Step 2: 替换端点映射**

将：

```csharp
app.MapHealthChecks("/health");
```

改为：

```csharp
app.MapSevenHealthChecks();
```

- [ ] **Step 3: 清理无用 using**

若不再直接使用，删除：

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
```

保留 `using Seven.WebApi;`（扩展方法所在命名空间；若已有则不动）。

- [ ] **Step 4: 构建整个解决方案相关项目**

```powershell
dotnet build Seven.Net8/Seven.WebApi/Seven.WebApi.csproj
```

Expected: Build succeeded，0 Error

---

### Task 5: 更新集成测试与文档

**Files:**
- Modify: `Seven.Net8/Seven.Tests/Integration/HealthApiTests.cs`
- Modify: `doc/02-后端开发指南.md`

**Interfaces:**
- Consumes: `/health`、`/health-ui`、`/api/Health`
- Produces: 测试覆盖新端点；文档描述与实现一致

- [ ] **Step 1: 更新 `HealthApiTests.cs`**

将文件改为：

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Seven.Domain.Common;

namespace Seven.Tests.Integration;

/// <summary>
/// 健康检查 API 集成测试
/// </summary>
public class HealthApiTests : IClassFixture<SevenWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthApiTests(SevenWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Health_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/api/Health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<WebResponseContent>();
        body.Should().NotBeNull();
        body!.Status.Should().BeTrue();
    }

    [Fact]
    public async Task HealthCheck_Endpoint_ShouldBeHealthy()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Healthy");
        json.Should().Contain("self");
        json.Should().Contain("database");
        json.Should().NotContain("infrastructure");
    }

    [Fact]
    public async Task HealthUi_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/health-ui");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

- [ ] **Step 2: 跑健康检查相关测试**

```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~HealthApiTests" --no-restore
```

若提示 restore：先 `dotnet restore Seven.Net8/Seven.Tests/Seven.Tests.csproj` 再测。

Expected: 3 passed

- [ ] **Step 3: 更新后端开发指南**

在 `doc/02-后端开发指南.md`：

1. 将 §2.1 第 7 条改为：

```markdown
7. **健康检查**：`AddSevenHealthChecks` → `/health`（`self`、`database`，以及按配置可选的 `redis` / `rabbitmq` / `minio`）；仪表盘 `/health-ui`（HealthChecks.UI + InMemory）。
```

2. 将管道列表中的：

```
MapHealthChecks /health
```

改为：

```
MapSevenHealthChecks  /health + /health-ui
```

- [ ] **Step 4: 全量测试（可选但推荐）**

```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj
```

Expected: 全部通过（若有无关失败，记录但不在本任务范围修复，除非由本次改动引起）

---

## Spec Coverage Checklist

| 规格要求 | Task |
|---------|------|
| 条件注册 Redis/MQ/MinIO | Task 3 |
| DB 按 Provider 社区包 | Task 3（非 Testing） |
| Testing 不因真实连接失败 | Task 3（`AddDbContextCheck`） |
| MinIO 只读不建桶 | Task 2 |
| `/health` + `UIResponseWriter` | Task 3–4 |
| `/health-ui` + InMemory | Task 3–4 |
| 删除 infrastructure 旧检查 | Task 2、4 |
| 文档与测试 | Task 5 |
| 包 9.0.0 | Task 1 |

## Self-Review Notes

- RabbitMQ 9.0 要求提供 `IConnection`（不再内部缓存连接串）；仅在 `Provider=RabbitMQ` 时注册 singleton，避免本地默认污染。
- `BuildAmqpUri` 必须 Escape vhost `/` → `%2F`。
- 若 `AddMySql(connectionString, name: "database")` 在 9.0 中参数名不同，以 IntelliSense/编译错误为准，**检查名必须仍为** `database` / `redis` / `rabbitmq` / `minio` / `self`。
