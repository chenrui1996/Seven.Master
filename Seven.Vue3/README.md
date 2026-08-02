# Seven.Vue3

Seven.Master 前端：Vue 3 + Vite + TypeScript + Pinia + Element Plus + vue-i18n。

## 开发

```bash
npm install
npm run dev
```

API 基址见 `.env.development`（`VITE_API_BASE_URL`）。启动时会请求 `GET /api/config/features` 裁剪菜单与入口。

## 构建

```bash
npm run build
npm run preview
```

Docker 镜像：`Dockerfile` + `docker/nginx-default.conf`（见 `deploy/k8s`）。

## `src` 目录

| 目录 | 说明 |
|------|------|
| `api/` | HTTP 客户端、登录/菜单/字典/上传 |
| `components/` | CrudPanel、AlarmBell、主题/语言切换 |
| `composables/` | SignalR Hub、表格列、菜单标签 |
| `directives/` | `v-permission` |
| `extension/` | 生成页业务扩展（不被代码生成覆盖） |
| `layout/` | 主布局、标签页、KeepAlive |
| `locales/` | 多语言 JSON |
| `router/` | 动态路由与守卫 |
| `stores/` | user / menu / tabs / dict / **features** / theme / locale / alarm |
| `views/system/` | 系统管理页 |
| `views/Business/` | 业务生成页 |

## 功能开关

与后端 `Features` 同步：关闭工作流/Quartz/告警/Builder 时对应菜单不出现；验证码、Hub、Home 广播区按需显示。  
详见 [doc/14-功能开关.md](../doc/14-功能开关.md)。

## 能力一览

- 动态路由 + RBAC + 按钮权限
- JWT + Refresh 自动续期
- 浅色 / 深色 / 跟随系统
- 多语言（zh-CN / en-US / ja-JP）
- 告警铃铛与系统通知（SignalR，可关）
- 生成页扩展按钮与 `submitAudit` 钩子

## 国际化脚本

| 命令 | 说明 |
|------|------|
| `npm run i18n:missing` | 列出缺失键 |
| `npm run i18n:translate` | 写入 en-US |
| `npm run i18n:translate:ja` | 写入 ja-JP |

## 文档

- [文档索引](../doc/README.md)
- [前端开发指南](../doc/03-前端开发指南.md)
- [国际化指南](../doc/08-国际化指南.md)
