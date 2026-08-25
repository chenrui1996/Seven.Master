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

