# Token 失效跳转登录页设计

状态：已实现（方案 1，2026-08-05；防重入标志在再次持有 token 时复位）

## 目标

延续现有双 Token + 静默刷新设计，修好 401 处理缺口：刷新最终失败时提示「登录已过期，请重新登录」，清登录态并回到登录页。

## 现状与缺口

现有 `Seven.Vue3/src/api/http.ts`：

- 请求拦截器附加 `Authorization: Bearer`
- 401 时优先 `POST /api/Auth/refresh`（单飞 + 排队重放）
- 无 refresh 或 refresh **抛错** 时 `logout` + `push('/login')`

缺口：

1. refresh 返回 `status === false` 时未登出、未跳转
2. 失败时未清空/reject `pendingRequests`
3. 无「登录已过期」提示；并发 401 可能多次跳转

## 行为

HTTP **401**（非白名单请求）时：

| 条件 | 行为 |
|------|------|
| 无 refreshToken | `forceLogoutToLogin()` |
| 有 refreshToken 且刷新成功 | `setToken`，重放排队请求 |
| 有 refreshToken 且刷新失败（抛错或 `status === false`） | `forceLogoutToLogin()`，reject 排队请求 |

白名单不变：`/api/Auth/login`、`/api/Auth/refresh`、`/api/Captcha/create`。

`forceLogoutToLogin()`：

1. 若 `isRedirectingToLogin` 已置位 → 直接 return（防重入）
2. 置位 → `ElMessage.error(i18n.t('login.expired'))` → `userStore.logout()` → 清空 pending 并 reject → `router.replace('/login')`

## 文案

新增 `login.expired`：

| 语言 | 文案 |
|------|------|
| zh-CN | 登录已过期，请重新登录 |
| en-US | Session expired. Please sign in again |
| ja-JP | ログインの有効期限が切れました。再度ログインしてください |

## 改动范围

- `Seven.Vue3/src/api/http.ts`：补全失败分支、防重入、统一登出跳转
- `Seven.Vue3/src/locales/lang/{zh-CN,en-US,ja-JP}.json`：`login.expired`
- 可选：同步更新 `doc/03-前端开发指南.md` §3.3 描述（提示 + `replace`）

## 明确不做

- 不改后端 JWT / Auth API
- 不改路由守卫
- 不改 `downloadFile` / `downloadGet` 独立 axios
- 不做硬跳转 `window.location`

## 验收

1. Access 过期、Refresh 仍有效 → 静默刷新，页面不跳转、无过期提示
2. Refresh 无效或接口失败 → 一次过期提示后进入 `/login`，本地 Token 已清
3. 并发多个 401 → 只弹一次提示、只跳一次登录页
