# Seven.Master 开发文档

Seven 是从 Legrand.Master（VolCore）迁移而来的 .NET 8 + Vue 3 企业级后台系统。

## 文档索引

| 文档 | 说明 |
|------|------|
| [01-快速开始](./01-快速开始.md) | 环境、配置、启动 |
| [02-后端开发指南](./02-后端开发指南.md) | 分层架构、新增模块流程 |
| [03-前端开发指南](./03-前端开发指南.md) | Vite 项目、路由、Axios |
| [04-权限与菜单](./04-权限与菜单.md) | RBAC、动态菜单、按钮权限 |
| [05-工作流](./05-工作流.md) | 审批流程 |
| [06-代码生成器](./06-代码生成器.md) | Builder 使用 |
| [07-部署指南](./07-部署指南.md) | Docker Compose 部署 |
| [08-国际化指南](./08-国际化指南.md) | 多语言配置、新增语言、自动翻译 |
| [09-告警模块](./09-告警模块.md) | 报警码配置、后端抛警、SignalR 推送 |
| [10-消息队列指南](./10-消息队列指南.md) | RabbitMQ + MassTransit 配置与使用 |

## 技术栈

- **后端**: .NET 8 + EF Core 8 + JWT + Redis + RabbitMQ + Quartz + MinIO + Serilog
- **前端**: Vue 3 + Vite + TypeScript + Pinia + Element Plus + vue-i18n

## 参考

文档结构参考 [Vol.Pro 开发文档](http://doc.volcore.xyz/)，实现遵循 [系统架构设计.md](../系统架构设计.md)。
