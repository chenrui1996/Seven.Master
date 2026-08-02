# Features 功能开关设计

状态：已批准（用户 2026-08-02）

## 目标

简单项目可通过 `appsettings.json` → `Features` 关闭复杂能力；后端不注册对应组件，前端隐藏菜单与入口。

## 配置

节名：`Features`。总开关与旧节细节为 **AND**（例如 `Features.MinIO && MinIO.Enabled`）。

默认偏向简单项目：WorkFlow/Quartz/MQ/Outbox/Mail/MinIO/Tenant/Captcha = false；SignalR/Alarm/RateLimit/Idempotency/DataScope/AuditInterceptor/Builder = true。

## API

`GET /api/config/features`（匿名）返回全部布尔开关。

## 前后端

- 后端：DI / 中间件 / Hub / Controller `[RequiresFeature]` 门控
- 前端：`useFeatureStore` + 菜单过滤 + 登录验证码/Hub 条件加载
