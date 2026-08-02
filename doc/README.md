# Seven.Master 开发文档

Seven 是从 Legrand.Master（VolCore）迁移而来的 .NET 8 + Vue 3 企业级后台系统。复杂能力（工作流、Quartz、MQ、多租户等）均可通过 [`Features`](./14-功能开关.md) 开关裁剪，适合从简单 CRUD 平滑扩展到完整平台。

## 文档索引

| 文档 | 说明 |
|------|------|
| [01-快速开始](./01-快速开始.md) | 环境、配置、启动；[如何查看日志](./01-快速开始.md#6-如何查看日志) |
| [02-后端开发指南](./02-后端开发指南.md) | 分层架构、项目/文件夹职责、[日志](./02-后端开发指南.md#日志)、新增模块 |
| [03-前端开发指南](./03-前端开发指南.md) | Vite 项目结构、路由、Store、功能开关 |
| [04-权限与菜单](./04-权限与菜单.md) | RBAC、动态菜单、按钮权限 |
| [05-工作流](./05-工作流.md) | 审批流程（需 `Features.WorkFlow`） |
| [06-代码生成器](./06-代码生成器.md) | Builder 使用（需 `Features.Builder`） |
| [07-部署指南](./07-部署指南.md) | Docker Compose / K8s / Nginx |
| [08-国际化指南](./08-国际化指南.md) | 多语言配置、新增语言、自动翻译 |
| [09-告警模块](./09-告警模块.md) | 报警码、抛警、SignalR（需 `Features.Alarm`） |
| [10-消息队列指南](./10-消息队列指南.md) | RabbitMQ + MassTransit / Outbox |
| [11-生成页自定义按钮](./11-生成页自定义按钮.md) | 工具栏/行内扩展、overlay、权限 |
| [12-生成页配置](./12-生成页配置.md) | 列设置、导入导出、查询、hooks、主子表 |
| [13-安全与数据权限](./13-安全与数据权限.md) | 验证码、限流、防重、API/数据权限、多租户 |
| [14-功能开关](./14-功能开关.md) | **Features 总控**：简单项目裁剪复杂能力 |

## 仓库布局

```
Seven.Master/
├── Seven.Net8/          # 后端解决方案（见 Seven.Net8/README.md）
├── Seven.Vue3/          # 前端（见 Seven.Vue3/README.md）
├── deploy/              # Nginx、K8s 样例清单
├── docker-compose.yml   # 本地/一体机编排
├── scripts/             # 辅助脚本
└── doc/                 # 本目录
```

## 技术栈

- **后端**: .NET 8 + EF Core 8 + JWT + Redis + RabbitMQ + Quartz + MinIO + Serilog（均可按 Features 裁剪）
- **前端**: Vue 3 + Vite + TypeScript + Pinia + Element Plus + vue-i18n

## 参考

文档结构参考 [Vol.Pro 开发文档](http://doc.volcore.xyz/)，实现遵循仓库根目录 [系统架构设计.md](../../系统架构设计.md)。
