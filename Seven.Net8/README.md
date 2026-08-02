# Seven.Net8 后端解决方案

打开 **`Seven.sln`** 加载全部子项目。启动项目：**Seven.WebApi**。

## 项目说明

| 项目 | 职责 |
|------|------|
| **Seven.Domain** | 实体、枚举、`BaseEntity`（审计/软删/TenantId）、统一响应 |
| **Seven.Application** | `I*Service`、DTO、MQ 契约；不依赖 EF |
| **Seven.Infrastructure** | EF Core、缓存、HotStore、JWT、Permission/DataScope、中间件、Quartz、MinIO、Mail、MassTransit/Outbox |
| **Seven.Business** | 业务域服务（工作流引擎等），`AddSevenBusiness()` |
| **Seven.Builder** | 代码生成器核心 |
| **Seven.WebApi** | HTTP/SignalR 入口、Swagger 分组、Controllers、生成模板 |
| **Seven.Tests** | 单元与集成测试 |

依赖：`WebApi → Application → Domain ← Infrastructure / Business / Builder`。

## Infrastructure 关键目录

| 目录 | 内容 |
|------|------|
| `Configuration` | Options（含 **Features**） |
| `Persistence` | DbContext、Migrations、DbSeeder、审计拦截器 |
| `Services` | 系统与业务 CRUD 服务 |
| `Security` | Token、验证码、`[Permission]`、`[RequiresFeature]`、数据权限 |
| `Middleware` | 异常/Trace/日志/白名单/防重 |
| `Caching` / `HotStore` / `Storage` / `Mail` | 缓存、热数据、文件、邮件 |
| `Messaging` / `Quartz` | MQ、Outbox、定时任务 |

## 常用命令

```bash
cd Seven.Net8
dotnet build Seven.sln
dotnet test Seven.sln
dotnet run --project Seven.WebApi
```

## 数据库迁移

```bash
dotnet tool restore
dotnet ef database update --project Seven.Infrastructure --startup-project Seven.WebApi
dotnet ef migrations add <Name> --project Seven.Infrastructure --startup-project Seven.WebApi
```

生产保持 `Database:MigrateOnStartup=false`，由流水线迁移。

## 功能开关

`Seven.WebApi/appsettings.json` → `Features`。简单项目保持默认即可。  
见 [doc/14-功能开关.md](../doc/14-功能开关.md)。

## 文档

- [快速开始](../doc/01-快速开始.md)
- [后端开发指南](../doc/02-后端开发指南.md)
- [文档索引](../doc/README.md)
