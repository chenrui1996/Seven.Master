# Seven.Net8 后端解决方案

打开 **`Seven.sln`** 即可加载全部子项目（结构与 Legrand.Net6/Legrand.sln 一致）。

## 项目对照

| Seven | Legrand | 说明 |
|-------|---------|------|
| Seven.Domain | Legrand.Entity | 实体与枚举 |
| Seven.Application | — | 服务接口层 |
| Seven.Infrastructure | Legrand.Core + Legrand.System | 基础设施实现 |
| Seven.Builder | Legrand.Builder | 代码生成器 |
| Seven.WebApi | Legrand.WebApi | API 入口（启动项目） |
| Seven.Business | Legrand.Board | 业务模块（原 Board） |
| Seven.Tests | — | 单元与集成测试 |

## 打开方式

双击 `Seven.Net8/Seven.sln`，在 Visual Studio 中右键 **Seven.WebApi** → **设为启动项目**，F5 运行。

- API: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- 默认账号: `admin` / `123456`

## 常用命令

```bash
cd Seven.Net8
dotnet build Seven.sln
dotnet test Seven.sln
dotnet run --project Seven.WebApi
```

## 数据库迁移

迁移文件：`Seven.Infrastructure/Migrations/`。启动时自动 `MigrateAsync()`。

```bash
dotnet tool restore
dotnet ef database update --project Seven.Infrastructure --startup-project Seven.WebApi
dotnet ef migrations add <Name> --project Seven.Infrastructure --startup-project Seven.WebApi
```

详见 [doc/01-快速开始.md](../doc/01-快速开始.md) §3。
