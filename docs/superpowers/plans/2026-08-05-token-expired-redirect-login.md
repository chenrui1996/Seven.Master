# Token 失效跳转登录页 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修好 Axios 401 处理缺口：静默刷新失败后提示「登录已过期，请重新登录」，清登录态并以 `replace` 回到登录页。

**Architecture:** 在现有 `http.ts` 响应拦截器内抽出 `forceLogoutToLogin()`，用模块级 `isRedirectingToLogin` 防重入；覆盖无 refresh、refresh 抛错、refresh `status === false` 三条失败路径，并 reject 排队请求。文案走 i18n `login.expired`。

**Tech Stack:** Vue 3、Axios、Pinia、vue-i18n、Vue Router、Element Plus

## Global Constraints

- 延续双 Token 静默刷新；失败才登出跳转（不做「一遇 401 就登录」）
- 提示文案：`登录已过期，请重新登录`（i18n key：`login.expired`）
- 跳转使用 `router.replace('/login')`，不用 `window.location`
- 不改后端、不改路由守卫、不改 `downloadFile` / `downloadGet`
- 提交：仅在用户明确要求时 `git commit`（本仓库用户规则优先于计划默认「frequent commits」）

---

## File Structure

| 文件 | 职责 |
|------|------|
| `Seven.Vue3/src/api/http.ts` | 401 刷新队列 + `forceLogoutToLogin` |
| `Seven.Vue3/src/locales/lang/zh-CN.json` | `login.expired` 中文 |
| `Seven.Vue3/src/locales/lang/en-US.json` | `login.expired` 英文 |
| `Seven.Vue3/src/locales/lang/ja-JP.json` | `login.expired` 日文 |
| `doc/03-前端开发指南.md` | 同步 §3.3 行为描述 |

无新建文件。本仓库 `Seven.Vue3` 无单元测试；各 Task 以手工验收代替自动化测试。

---

### Task 1: 增加 `login.expired` 三语文案

**Files:**
- Modify: `Seven.Master/Seven.Vue3/src/locales/lang/zh-CN.json`
- Modify: `Seven.Master/Seven.Vue3/src/locales/lang/en-US.json`
- Modify: `Seven.Master/Seven.Vue3/src/locales/lang/ja-JP.json`

**Interfaces:**
- Consumes: 无
- Produces: i18n key `login.expired`（三语言）

- [ ] **Step 1: 在 zh-CN `login` 对象末尾增加键**

在 `requestFailed` 后增加：

```json
"requestFailed": "登录请求失败",
"expired": "登录已过期，请重新登录"
```

- [ ] **Step 2: 在 en-US `login` 对象末尾增加键**

```json
"requestFailed": "Sign in request failed",
"expired": "Session expired. Please sign in again"
```

- [ ] **Step 3: 在 ja-JP `login` 对象末尾增加键**

```json
"requestFailed": "ログイン要求失敗",
"expired": "ログインの有効期限が切れました。再度ログインしてください"
```

- [ ] **Step 4: 手工核对 JSON 合法**

在 `Seven.Master/Seven.Vue3` 目录运行：

```powershell
node -e "JSON.parse(require('fs').readFileSync('src/locales/lang/zh-CN.json','utf8')); JSON.parse(require('fs').readFileSync('src/locales/lang/en-US.json','utf8')); JSON.parse(require('fs').readFileSync('src/locales/lang/ja-JP.json','utf8')); console.log('ok')"
```

Expected: 打印 `ok`，无 SyntaxError

---

### Task 2: 补全 `http.ts` 401 收口

**Files:**
- Modify: `Seven.Master/Seven.Vue3/src/api/http.ts`

**Interfaces:**
- Consumes: `login.expired`（Task 1）；`useUserStore().logout` / `setToken`；`router.replace`
- Produces: 模块内 `forceLogoutToLogin()`；401 失败路径统一调用它

- [ ] **Step 1: 增加 i18n 导入与防重入标志**

在文件顶部现有 import 旁增加：

```ts
import i18n from '../locales'
```

在 `let isRefreshing = false` 旁增加：

```ts
let isRedirectingToLogin = false
```

- [ ] **Step 2: 改排队类型，实现 `forceLogoutToLogin`，替换错误拦截器**

将：

```ts
let pendingRequests: Array<(token: string) => void> = []
```

改为：

```ts
type PendingHandler = {
  resolve: (token: string) => void
  reject: (err: unknown) => void
}
let pendingRequests: PendingHandler[] = []
```

在拦截器之前增加：

```ts
function forceLogoutToLogin(error: unknown) {
  if (isRedirectingToLogin) return Promise.reject(error)
  isRedirectingToLogin = true
  ElMessage.error(i18n.global.t('login.expired'))
  useUserStore().logout()
  if (router.currentRoute.value.path !== '/login') {
    router.replace('/login')
  }
  return Promise.reject(error)
}
```

将 `http.interceptors.response.use` 的第二个参数（错误处理器）整段替换为：

```ts
async (error) => {
  const originalRequest = error.config
  if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {
    const userStore = useUserStore()
    if (!userStore.refreshToken) {
      return forceLogoutToLogin(error)
    }
    if (isRefreshing) {
      return new Promise((resolve, reject) => {
        pendingRequests.push({
          resolve: (token: string) => {
            originalRequest.headers.Authorization = `Bearer ${token}`
            resolve(http(originalRequest))
          },
          reject,
        })
      })
    }
    originalRequest._retry = true
    isRefreshing = true
    try {
      const res = await axios.post<ApiResponse>(
        `${import.meta.env.VITE_API_BASE_URL}/api/Auth/refresh`,
        { refreshToken: userStore.refreshToken }
      )
      if (res.data.status && res.data.data) {
        const data = res.data.data as { token: string; refreshToken: string }
        userStore.setToken(data.token, data.refreshToken)
        const queued = pendingRequests
        pendingRequests = []
        queued.forEach((p) => p.resolve(data.token))
        originalRequest.headers.Authorization = `Bearer ${data.token}`
        return http(originalRequest)
      }
      const queued = pendingRequests
      pendingRequests = []
      queued.forEach((p) => p.reject(error))
      return forceLogoutToLogin(error)
    } catch (refreshError) {
      const queued = pendingRequests
      pendingRequests = []
      queued.forEach((p) => p.reject(refreshError))
      return forceLogoutToLogin(refreshError)
    } finally {
      isRefreshing = false
    }
  }
  return Promise.reject(error)
}
```

成功拦截器与 `responseErrorWhitelist` 保持不变。

- [ ] **Step 3: TypeScript 检查**

在 `Seven.Master/Seven.Vue3` 运行：

```powershell
npx vue-tsc --noEmit
```

Expected: 无与 `http.ts` / locales 相关的新增错误（若项目原本有无关错误，记录但不阻断，只要本改动不引入新错）

- [ ] **Step 4: 手工验收（开发服务器已在跑则可直接测）**

验收步骤：

1. 正常登录后，在 DevTools Application 将 `token`（access）改成无效值，保留合法 `refreshToken`，触发任意需鉴权接口 → **应静默刷新成功**，无过期提示、不跳登录页
2. 将 `token` 与 `refreshToken` 都改成无效值，再触发需鉴权接口 → **弹出一次**「登录已过期，请重新登录」，并进入 `/login`，localStorage 中 token/refreshToken 已清除
3. 登录后删除 `refreshToken`，改坏 `token`，触发接口 → 同步骤 2
4. 快速连续触发多个需鉴权请求（或 Network 里看并发）→ 只弹 **一次** 提示、只跳 **一次**

---

### Task 3: 同步前端开发指南 §3.3

**Files:**
- Modify: `Seven.Master/doc/03-前端开发指南.md`（约 §3.3）

**Interfaces:**
- Consumes: Task 2 最终行为
- Produces: 文档与实现一致

- [ ] **Step 1: 更新 §3.3 条目**

将：

```markdown
1. 请求拦截器附加 `Authorization: Bearer {token}`
2. 收到 **HTTP 401**：
   - 无 refresh → `logout` + `/login`
   - 有 refresh → 单飞刷新：`POST /api/Auth/refresh`，成功则 `setToken` 并重放队列中的请求
   - 刷新失败 → 登出
3. 登出：清 Token/权限，并 `clearDynamicRoutes()`；布局侧通常再清空 `menuStore`
```

替换为：

```markdown
1. 请求拦截器附加 `Authorization: Bearer {token}`
2. 收到 **HTTP 401**：
   - 无 refresh → 提示 `login.expired` → `logout` → `router.replace('/login')`（防重入，只提示/跳转一次）
   - 有 refresh → 单飞刷新：`POST /api/Auth/refresh`，成功则 `setToken` 并重放队列中的请求
   - 刷新失败（抛错或 `status === false`）→ 同上提示并登出跳转，同时 reject 排队请求
3. 登出：清 Token/权限，并 `clearDynamicRoutes()`；布局侧通常再清空 `menuStore`
```

- [ ] **Step 2: 目视核对文档与 `http.ts` 行为一致**

对照设计文档：`docs/superpowers/specs/2026-08-05-token-expired-redirect-login-design.md`

---

## Spec Coverage Checklist

| 规格要求 | Task |
|----------|------|
| 无 refresh → 提示 + logout + 登录页 | Task 2 |
| refresh 成功 → 静默重放 | Task 2（保留） |
| refresh 抛错 / `status === false` → 提示 + 跳转 | Task 2 |
| reject 排队请求 | Task 2 |
| 防重入只弹一次 | Task 2 `isRedirectingToLogin` |
| `login.expired` 三语言 | Task 1 |
| `replace` 非硬跳转 | Task 2 |
| 不改后端 / 守卫 / download | 全局约束 |
| 文档 §3.3 | Task 3 |
