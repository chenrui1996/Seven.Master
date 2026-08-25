# HealthChecks 迁至 AspNetCore.Diagnostics.HealthChecks

状态：已批准（用户 2026-08-25）

## 目标

用 Xabaril `AspNetCore.HealthChecks.*` 社区包替换 `Seven.WebApi` 自写 Redis/RabbitMQ/DB+缓存探针，并接入 HealthChecks.UI 仪表盘。未启用的可选依赖不注册检查项。

## 决策摘要

| 主题 | 决策 |
|---|---|
| 未启用依赖 | 不注册对应检查（响应中无该项） |
| DB + 缓存 | 拆开：DB 按 Provider 用社区包；Redis 仅 `Cache.Provider=Redis` 时 `AddRedis`；不再做 Memory 写读探针 |
| MinIO | 无成熟社区包；保留精简自定义检查，只读 `BucketExists`，不建桶 |
| UI | HealthChecks.UI + InMemory 存储；`/health-ui` |
| K8s | 探针路径仍为 `/health` |

## 架构

```
Program.cs
  └─ AddSevenHealthChecks(configuration)
       ├─ AddHealthChecks()
       │    ├─ self（始终）
       │    ├─ database：Database.Provider → AddMySql / AddSqlServer / AddNpgSql
       │    ├─ redis：仅 Cache.Provider=Redis 时 AddRedis
       │    ├─ rabbitmq：仅 MessageQueue.Provider=RabbitMQ 时 AddRabbitMQ
       │    └─ minio：仅 MinIO.Enabled 时 MinioHealthCheck
       └─ AddHealthChecksUI(...).AddInMemoryStorage()

端点：
  /health      MapHealthChecks + UIResponseWriter（K8s 探针）
  /health-ui   MapHealthChecksUI
  /api/Health  现有轻量 Controller，不动
```

检查名：`self`、`database`（取代 `infrastructure`）、条件性 `redis` / `rabbitmq` / `minio`。

UI 轮询本机 `/health`；`/health` 与 `/health-ui` 保持匿名。

## 包清单（Seven.WebApi，统一 9.0.0）

- `AspNetCore.HealthChecks.MySql`
- `AspNetCore.HealthChecks.SqlServer`
- `AspNetCore.HealthChecks.NpgSql`
- `AspNetCore.HealthChecks.Redis`
- `AspNetCore.HealthChecks.Rabbitmq`
- `AspNetCore.HealthChecks.UI`
- `AspNetCore.HealthChecks.UI.Client`
- `AspNetCore.HealthChecks.UI.InMemory.Storage`

## 注册规则

读现有 Options，不新增配置节。

- **database**：始终注册；用 `Database.ConnectionString` + `Provider` 分支
- **redis**：`Cache.Provider == Redis` 且连接串非空 → `AddRedis`
- **rabbitmq**：`MessageQueue.Provider == RabbitMQ` → `AddRabbitMQ`（由 Host/Port/VHost/User/Pass 拼连接串）
- **minio**：`MinIO.Enabled == true` → `AddCheck<MinioHealthCheck>("minio")`
- **UI**：`AddHealthCheckEndpoint("seven-api", "/health")`；评估间隔约 15 秒

## 错误处理

| 场景 | 结果 |
|---|---|
| 依赖可达 | Healthy，HTTP 200 |
| 已注册但不可达 | Unhealthy，`/health` 非 200 |
| 未启用 Redis/MQ/MinIO | 不注册 |
| MinIO 启用但桶不存在 | Degraded（不建桶）；默认仍 200 |
| UI 故障 | 不影响 `/health` 探针 |

## 文件变更

| 操作 | 路径 |
|---|---|
| 改 | `Seven.Net8/Seven.WebApi/Seven.WebApi.csproj` |
| 增 | `Seven.Net8/Seven.WebApi/HealthChecksExtensions.cs` |
| 改 | `Seven.Net8/Seven.WebApi/Program.cs` |
| 改 | `Seven.Net8/Seven.WebApi/OptionalInfrastructureHealthChecks.cs`（仅只读 MinIO） |
| 删 | `Seven.Net8/Seven.WebApi/SevenInfrastructureHealthCheck.cs` |
| 改 | `Seven.Net8/Seven.Tests/Integration/HealthApiTests.cs`（必要时） |
| 改 | `doc/02-后端开发指南.md` |

## 范围外

- 不改 K8s 探针路径
- 不用业务库作 UI 存储
- 不为 `/health` / `/health-ui` 加鉴权
- 不拆 liveness / readiness 双端点
