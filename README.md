# Seven.Master

从 Legrand.Master 迁移的企业级后台系统（.NET 8 + Vue 3）。

## 快速启动

### 后端

打开 `Seven.Net8/Seven.sln` 即可加载全部模块，或：

```bash
cd Seven.Net8
dotnet run --project Seven.WebApi
```

- API: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- 默认账号: `admin` / `123456`

### 前端

```bash
cd Seven.Vue3
npm install
npm run dev
```

- 前端: http://localhost:5173

### Docker

```bash
docker compose up -d
```

## 项目结构

| 目录 | 说明 |
|------|------|
| `Seven.Net8/` | 后端 .NET 8 解决方案 |
| `Seven.Vue3/` | 前端 Vite + Vue 3 + Pinia |
| `scripts/` | 数据迁移脚本 |
| `../doc/` | 使用文档 |
| `../系统迁移实现大纲.md` | 迁移设计文档 |

## 核心改造

- BCrypt 密码哈希（替代 DES）
- JWT + Refresh Token
- 延迟双删缓存（Memory/Redis 可切换）
- EF Core 8 CodeFirst
- Serilog 日志
- SignalR 实时推送
- RabbitMQ + MassTransit 消息队列（与告警模块集成）
- MinIO/本地文件存储

## 文档

详见 [doc/README.md](../doc/README.md)
