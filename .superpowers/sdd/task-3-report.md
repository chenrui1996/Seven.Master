# Task 3 Report: 同步前端开发指南 §3.3

**Date:** 2026-08-05  
**Status:** 完成  
**Git commit:** 未执行（按任务要求）

## 变更文件

| 文件 | 操作 |
|------|------|
| `doc/03-前端开发指南.md` | 更新 §3.3 Access / Refresh（用法） |

## Step 1 — §3.3 更新内容

已将旧版「无 refresh → logout + /login」「刷新失败 → 登出」替换为与 Task 2 一致的描述：

- 无 refresh / 刷新失败：`login.expired` → `logout` → `router.replace('/login')`，防重入
- 刷新失败分支明确包含「抛错或 `status === false`」及 reject 排队请求
- 请求拦截器补充：重新登录后 token 存在时重置 `isRedirectingToLogin`

§3.3 行为表述与设计 doc「行为」表及 `forceLogoutToLogin` 步骤一致；401 白名单仍由 §3.4 说明，未在 §3.3 重复。

## Step 2 — 与 `http.ts` / 设计文档核对

| 设计文档 / 实现要点 | `Seven.Vue3/src/api/http.ts` | §3.3 文档 |
|---------------------|------------------------------|-----------|
| 无 refresh → `forceLogoutToLogin()` | L68–69 | ✓ |
| 刷新成功 → setToken + 重放 | L89–96 | ✓ |
| `status === false` → reject 队列 + forceLogout | L98–101 | ✓ |
| refresh 抛错 → reject 队列 + forceLogout | L102–106 | ✓ |
| `isRedirectingToLogin` 防重入 | L41–43 | ✓ |
| `router.replace('/login')` | L46–47 | ✓ |
| `login.expired` i18n | L44 | ✓（文案见 Task 1 locale） |
| 白名单三路径 | L52, L66 条件 | ✓ |
| token 存在时重置 `isRedirectingToLogin` | L26–27 | ✓（Human-approved extra） |

设计文档「明确不做」项（不改守卫、download 独立 axios、硬跳转）未在 §3.3 重复展开，与原文档风格一致。

## Spec Coverage（Task 3 范围）

| 规格要求 | 文档 §3.3 |
|----------|-----------|
| 无 refresh → 提示 + logout + 登录页 | ✓ |
| refresh 成功 → 静默重放 | ✓ |
| refresh 失败 → 提示 + 跳转 + reject 排队 | ✓ |
| 防重入 / replace | ✓ |
| `login.expired` | ✓（键名） |

## 关注点

- §3.4 仍描述业务 `status === false` Toast；与 §3.3 中 refresh 接口体 `status === false` 走登出分支不矛盾（refresh 在 401 响应错误拦截器内单独处理）。
- `downloadFile` / `downloadGet` 未使用主 `http` 实例，§3.3 未单独说明（与设计 doc「不改 download」一致）。

## 验收建议（文档层）

人工阅读 `doc/03-前端开发指南.md` §3.3 与 `docs/superpowers/specs/2026-08-05-token-expired-redirect-login-design.md` 行为表，应一一对应。
