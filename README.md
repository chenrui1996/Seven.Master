# Seven.Master

企业级后台（.NET 8 + Vue 3）。能力可按场景裁剪：极简 CRUD 只需打开数据库与 JWT；工作流、Quartz、MQ、多租户等通过 **`Features`** 开关按需启用。

## 快速启动

### 后端

```bash
cd Seven.Net8
dotnet run --project Seven.WebApi
```

- API: http://localhost:5000  
- Swagger: http://localhost:5000/swagger（开发环境）  
- 默认账号: `admin` / `123456`  
- 功能开关: `GET /api/config/features`

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

## 仓库结构

| 目录 | 说明 |
|------|------|
| `Seven.Net8/` | 后端解决方案（Domain / Application / Infrastructure / Business / Builder / WebApi / Tests） |
| `Seven.Vue3/` | 前端 Vite + Vue 3 + Pinia |
| `deploy/` | Nginx 反代样例、K8s Deployment/Service/Ingress |
| `doc/` | 开发文档（含 [功能开关](doc/14-功能开关.md)） |
| `docker-compose.yml` | MySQL / Redis / RabbitMQ / MinIO / API |
| `scripts/` | 辅助脚本 |

## 功能开关（摘要）

编辑 `Seven.Net8/Seven.WebApi/appsettings.json` → `Features`：

- 默认关闭：WorkFlow、Quartz、MessageQueue、Outbox、Mail、MinIO、Tenant、Captcha  
- 默认开启：SignalR、Alarm、RateLimit、Idempotency、DataScope、AuditInterceptor、Builder  

完整说明：[doc/14-功能开关.md](doc/14-功能开关.md)

## 文档

- [文档索引](doc/README.md)
- [快速开始](doc/01-快速开始.md)
- [后端指南](doc/02-后端开发指南.md)
- [前端指南](doc/03-前端开发指南.md)
