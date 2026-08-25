# Task 2 报告：补全 http.ts 401 收口

## 状态

**已完成** — 实现与 `task-2-brief.md` 一致，未提交 git。

## 变更文件

- `Seven.Vue3/src/api/http.ts`

## 实现摘要

1. **导入与标志**：`import i18n from '../locales'`；`isRedirectingToLogin`。
2. **排队类型**：`PendingHandler`（`resolve` / `reject`），刷新失败时可 reject 等待中的请求。
3. **`forceLogoutToLogin`**：防重入 → `ElMessage.error(i18n.global.t('login.expired'))` → `logout()` → 非 `/login` 时 `router.replace('/login')` → `Promise.reject(error)`。
4. **401 错误拦截器**：
   - 无 `refreshToken` → `forceLogoutToLogin`。
   - 刷新中 → 入队，成功 resolve 重试，失败 reject。
   - 刷新成功 → 批量 `resolve` 队列并重试原请求。
   - 刷新响应无效或 catch → 队列 `reject` 后 `forceLogoutToLogin`。
   - 增加 `originalRequest` 存在性检查。
5. **未改动**：成功拦截器、`responseErrorWhitelist`、`downloadFile` / `downloadGet`。

## TypeScript 检查

```powershell
Set-Location Seven.Master/Seven.Vue3
npx vue-tsc --noEmit
```

- **结果**：exit code 0，无输出。
- **结论**：本次改动未引入新的 TS 错误。

## 手工验收（brief Step 4）

未在本环境执行浏览器 DevTools 验收（子 agent 无强制浏览器驱动）。

**建议 controller 验证：**

| 步骤 | 预期 |
|------|------|
| 无效 token + 合法 refreshToken，调鉴权接口 | 静默刷新，无过期 toast，不跳登录 |
| token 与 refreshToken 均无效 | 一次「登录已过期…」，进 `/login`，localStorage 清空 |
| 删 refreshToken、改坏 token | 同上一行 |
| 并发多个鉴权请求且 refresh 失败 | 仅一次 toast、一次跳转（`isRedirectingToLogin`） |

开发服务器若已在跑（`npm run dev`），可直接按 brief 四步测。

## 自检

- [x] 与 brief 代码结构一致（含 `queued` 快照再清空队列，避免竞态）。
- [x] 未使用 `window.location` 或 `router.push` 登出路径。
- [x] 401 仍优先尝试 refresh，仅无 token / 刷新失败走强制登出。
- [x] Linter：`http.ts` 无新增问题。
- [ ] 浏览器四步验收留待 controller（可选）。

## 风险 / 说明

- `isRedirectingToLogin` 在成功再次登录后不会重置；若用户登出后再次 401 需依赖整页重载或新会话 — 与 brief 设计一致（防并发重复 toast/跳转）。
- 下载接口仍走裸 `axios`，401 行为不在本任务范围（约束明确不改 download）。

## Commits

无（按任务要求未 commit）。

---

## Review 修复（Important）：`isRedirectingToLogin` SPA 重登后未重置

**日期**：2026-08-05

### 问题

`forceLogoutToLogin` 将 `isRedirectingToLogin = true` 后从未清除；SPA 内再次登录（无整页刷新）后，后续 401 在 `forceLogoutToLogin` 入口直接 `Promise.reject`，无 toast / logout / `replace`。

### 修复

- **文件**：`Seven.Vue3/src/api/http.ts`
- **变更**：在 **request** 拦截器中，当 `userStore.token` 存在时，在附加 `Authorization` **之前**执行 `isRedirectingToLogin = false`。
- **行为**：成功重登后的首个（及后续）带 token 请求会清除防重入标志；登出窗口内并发 401 仍见 `true`，防重复 toast/跳转不变。

### TypeScript 检查

```powershell
Set-Location Seven.Master/Seven.Vue3
npx vue-tsc --noEmit
```

- **结果**：exit code 0，无输出。

### 备注

- 未改 download 辅助、后端及其他文件；未 git commit。
- 原报告「风险/说明」中「成功再次登录后不会重置」已由此修复作废。
