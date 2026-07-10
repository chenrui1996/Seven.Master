# Seven.Vue3

Seven.Master 前端：Vue 3 + Vite + TypeScript + Pinia + Element Plus。

## 开发

```bash
npm install
npm run dev
```

默认 API 地址见 `.env.development`（`VITE_API_BASE_URL`）。

## 构建

```bash
npm run build
npm run preview
```

## 功能特性

- 动态路由与 RBAC 菜单（`/api/Sys_Menu/getMenu`）
- JWT + Refresh Token 自动续期
- 浅色 / 深色 / 跟随系统主题（`ThemeToggle`）
- **多语言切换**（vue-i18n，`LocaleSwitch`）
- **告警模块**（SignalR 实时推送，`AlarmBell`）
- 工业风 WMS/WCS UI（`src/styles/theme.css`）

## 国际化

| 命令 | 说明 |
|------|------|
| `npm run i18n:missing` | 列出 en-US 相对 zh-CN 缺失的键 |
| `npm run i18n:translate` | 自动翻译并写入 en-US |
| `npm run i18n:translate:ja` | 自动翻译并写入 ja-JP |

配置翻译、新增语言、自动翻译 API 详见：[doc/08-国际化指南.md](../doc/08-国际化指南.md)

告警模块详见：[doc/09-告警模块.md](../doc/09-告警模块.md)

## 文档

- [开发文档索引](../doc/README.md)
- [前端开发指南](../doc/03-前端开发指南.md)
