# Final Review Package — token-expired-redirect-login
Branch: feature/token-expired-redirect-login
Note: uncommitted working-tree changes (user rule: no auto-commit)
## Status
## feature/token-expired-redirect-login
 M Seven.Vue3/src/api/http.ts
 M Seven.Vue3/src/locales/lang/en-US.json
 M Seven.Vue3/src/locales/lang/ja-JP.json
 M Seven.Vue3/src/locales/lang/zh-CN.json
 M "doc/03-\345\211\215\347\253\257\345\274\200\345\217\221\346\214\207\345\215\227.md"
?? .superpowers/
?? docs/superpowers/plans/
?? docs/superpowers/specs/2026-08-05-token-expired-redirect-login-design.md

## Stat
 Seven.Vue3/src/api/http.ts                         | 59 ++++++++++++++++------
 Seven.Vue3/src/locales/lang/en-US.json             |  3 +-
 Seven.Vue3/src/locales/lang/ja-JP.json             |  3 +-
 Seven.Vue3/src/locales/lang/zh-CN.json             |  3 +-
 ...274\200\345\217\221\346\214\207\345\215\227.md" |  6 +--
 5 files changed, 52 insertions(+), 22 deletions(-)

## Diff
diff --git a/Seven.Vue3/src/api/http.ts b/Seven.Vue3/src/api/http.ts
index efcf9a8..2995449 100644
--- a/Seven.Vue3/src/api/http.ts
+++ b/Seven.Vue3/src/api/http.ts
@@ -1,13 +1,14 @@
 import axios from 'axios'
 import type { AxiosInstance, InternalAxiosRequestConfig } from 'axios'
 import { ElMessage } from 'element-plus'
 import { useUserStore } from '../stores/user'
 import router from '../router'
+import i18n from '../locales'
 
 /** 统一 API 响应结构 */
 export interface ApiResponse<T = unknown> {
   status: boolean
   message?: string
   data?: T
   code?: string
 }
@@ -18,70 +19,96 @@ const http = axios.create({
 }) as AxiosInstance & {
   get<T = ApiResponse>(url: string): Promise<T>
   post<T = ApiResponse>(url: string, data?: unknown): Promise<T>
 }
 
 http.interceptors.request.use((config: InternalAxiosRequestConfig) => {
   const userStore = useUserStore()
   if (userStore.token) {
+    isRedirectingToLogin = false
     config.headers.Authorization = `Bearer ${userStore.token}`
   }
   return config
 })
 
 let isRefreshing = false
-let pendingRequests: Array<(token: string) => void> = []
+let isRedirectingToLogin = false
+type PendingHandler = {
+  resolve: (token: string) => void
+  reject: (err: unknown) => void
+}
+let pendingRequests: PendingHandler[] = []
+
+function forceLogoutToLogin(error: unknown) {
+  if (isRedirectingToLogin) return Promise.reject(error)
+  isRedirectingToLogin = true
+  ElMessage.error(i18n.global.t('login.expired'))
+  useUserStore().logout()
+  if (router.currentRoute.value.path !== '/login') {
+    router.replace('/login')
+  }
+  return Promise.reject(error)
+}
 
 const responseErrorWhitelist = ['/api/Auth/login', '/api/Auth/refresh', '/api/Captcha/create']
 
 http.interceptors.response.use(
   (response) => {
     const data = response.data as ApiResponse
     const url = response.config.url ?? ''
     const skipToast = responseErrorWhitelist.some((p) => url.includes(p))
     if (!skipToast && data?.status === false && data.message) {
       ElMessage.error(data.message)
     }
     return data
   },
   async (error) => {
     const originalRequest = error.config
-    if (error.response?.status === 401 && !originalRequest._retry) {
+    if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {
       const userStore = useUserStore()
       if (!userStore.refreshToken) {
-        userStore.logout()
-        router.push('/login')
-        return Promise.reject(error)
+        return forceLogoutToLogin(error)
       }
       if (isRefreshing) {
-        return new Promise((resolve) => {
-          pendingRequests.push((token: string) => {
-            originalRequest.headers.Authorization = `Bearer ${token}`
-            resolve(http(originalRequest))
+        return new Promise((resolve, reject) => {
+          pendingRequests.push({
+            resolve: (token: string) => {
+              originalRequest.headers.Authorization = `Bearer ${token}`
+              resolve(http(originalRequest))
+            },
+            reject,
           })
         })
       }
       originalRequest._retry = true
       isRefreshing = true
       try {
-        const res = await axios.post<ApiResponse>(`${import.meta.env.VITE_API_BASE_URL}/api/Auth/refresh`, {
-          refreshToken: userStore.refreshToken
-        })
+        const res = await axios.post<ApiResponse>(
+          `${import.meta.env.VITE_API_BASE_URL}/api/Auth/refresh`,
+          { refreshToken: userStore.refreshToken }
+        )
         if (res.data.status && res.data.data) {
           const data = res.data.data as { token: string; refreshToken: string }
           userStore.setToken(data.token, data.refreshToken)
-          pendingRequests.forEach((cb) => cb(data.token))
+          const queued = pendingRequests
           pendingRequests = []
+          queued.forEach((p) => p.resolve(data.token))
           originalRequest.headers.Authorization = `Bearer ${data.token}`
           return http(originalRequest)
         }
-      } catch {
-        userStore.logout()
-        router.push('/login')
+        const queued = pendingRequests
+        pendingRequests = []
+        queued.forEach((p) => p.reject(error))
+        return forceLogoutToLogin(error)
+      } catch (refreshError) {
+        const queued = pendingRequests
+        pendingRequests = []
+        queued.forEach((p) => p.reject(refreshError))
+        return forceLogoutToLogin(refreshError)
       } finally {
         isRefreshing = false
       }
     }
     return Promise.reject(error)
   }
 )
 
diff --git a/Seven.Vue3/src/locales/lang/en-US.json b/Seven.Vue3/src/locales/lang/en-US.json
index 7708166..8f1a9fd 100644
--- a/Seven.Vue3/src/locales/lang/en-US.json
+++ b/Seven.Vue3/src/locales/lang/en-US.json
@@ -51,17 +51,18 @@
     "userNamePlaceholder": "Operator account",
     "passwordPlaceholder": "Password",
     "captcha": "Captcha",
     "captchaPlaceholder": "Enter the code shown",
     "captchaRefresh": "Click to refresh captcha",
     "submit": "Enter Dashboard",
     "success": "Signed in successfully",
     "failed": "Sign in failed",
-    "requestFailed": "Sign in request failed"
+    "requestFailed": "Sign in request failed",
+    "expired": "Session expired. Please sign in again"
   },
   "home": {
     "title": "Master Dashboard",
     "subtitle": "Business operations & system overview",
     "systemOk": "System Normal",
     "kpiInventory": "Inventory Qty",
     "kpiInventoryMeta": "Available quantity total",
     "kpiOccupancy": "Slot Occupancy",
diff --git a/Seven.Vue3/src/locales/lang/ja-JP.json b/Seven.Vue3/src/locales/lang/ja-JP.json
index 2295ddc..593405f 100644
--- a/Seven.Vue3/src/locales/lang/ja-JP.json
+++ b/Seven.Vue3/src/locales/lang/ja-JP.json
@@ -48,17 +48,18 @@
     "panelSubtitle": "オペレーターアカウントを入力",
     "userName": "ユーザー名",
     "password": "パスワード",
     "userNamePlaceholder": "オペレーターアカウント",
     "passwordPlaceholder": "パスワード",
     "submit": "ダッシュボードへ",
     "success": "ログイン成功",
     "failed": "ログイン失敗",
-    "requestFailed": "ログイン要求失敗"
+    "requestFailed": "ログイン要求失敗",
+    "expired": "ログインの有効期限が切れました。再度ログインしてください"
   },
   "home": {
     "title": "Master ダッシュボード",
     "subtitle": "業務稼働とシステム概要",
     "systemOk": "システム正常",
     "kpiInventory": "在庫数量",
     "kpiInventoryMeta": "出荷可能数量の合計",
     "kpiOccupancy": "棚占有率",
diff --git a/Seven.Vue3/src/locales/lang/zh-CN.json b/Seven.Vue3/src/locales/lang/zh-CN.json
index f11b9d5..5ba9bdf 100644
--- a/Seven.Vue3/src/locales/lang/zh-CN.json
+++ b/Seven.Vue3/src/locales/lang/zh-CN.json
@@ -51,17 +51,18 @@
     "userNamePlaceholder": "操作员账号",
     "passwordPlaceholder": "登录密码",
     "captcha": "验证码",
     "captchaPlaceholder": "请输入右侧验证码",
     "captchaRefresh": "点击刷新验证码",
     "submit": "进入控制台",
     "success": "登录成功",
     "failed": "登录失败",
-    "requestFailed": "登录请求失败"
+    "requestFailed": "登录请求失败",
+    "expired": "登录已过期，请重新登录"
   },
   "home": {
     "title": "Master 控制台",
     "subtitle": "业务运行与系统状态概览",
     "systemOk": "系统正常",
     "kpiInventory": "库存总量",
     "kpiInventoryMeta": "当前可发数量汇总",
     "kpiOccupancy": "货位占用率",
diff --git "a/doc/03-\345\211\215\347\253\257\345\274\200\345\217\221\346\214\207\345\215\227.md" "b/doc/03-\345\211\215\347\253\257\345\274\200\345\217\221\346\214\207\345\215\227.md"
index 8c225b5..8e96198 100644
--- "a/doc/03-\345\211\215\347\253\257\345\274\200\345\217\221\346\214\207\345\215\227.md"
+++ "b/doc/03-\345\211\215\347\253\257\345\274\200\345\217\221\346\214\207\345\215\227.md"
@@ -117,21 +117,21 @@ Pinia persist **只**保存：`token`、`refreshToken`、`userId`、`userName`
 **不持久化 `permissions`**：进入系统时守卫调用 `GET /api/Auth/permissions` 刷新，避免本地缓存导致已收回的按钮仍显示。
 
 `isLoggedIn` 仅看是否有 Access Token。刷新页面后菜单会重新拉取；标签页状态**不**持久化（刷新会丢 tab）。
 
 没有独立的「记住我」或 OAuth 登录；架构文档中的 Cookie / OAuth2 未做。
 
 ### 3.3 Access / Refresh（用法）
 
-1. 请求拦截器附加 `Authorization: Bearer {token}`
+1. 请求拦截器附加 `Authorization: Bearer {token}`；若已有 token（如重新登录后），重置 `isRedirectingToLogin`，避免防重入标志残留
 2. 收到 **HTTP 401**：
-   - 无 refresh → `logout` + `/login`
+   - 无 refresh → 提示 `login.expired` → `logout` → `router.replace('/login')`（防重入，只提示/跳转一次）
    - 有 refresh → 单飞刷新：`POST /api/Auth/refresh`，成功则 `setToken` 并重放队列中的请求
-   - 刷新失败 → 登出
+   - 刷新失败（抛错或 `status === false`）→ 同上提示并登出跳转，同时 reject 排队请求
 3. 登出：清 Token/权限，并 `clearDynamicRoutes()`；布局侧通常再清空 `menuStore`
 
 后端 Refresh 为**轮换制**（旧 refresh 立即失效），前端必须同时保存返回的新 `refreshToken`。
 
 ### 3.4 业务失败 vs HTTP 失败
 
 响应拦截器对 JSON 体：`status === false` 且有 `message` → `ElMessage.error`。  
 白名单（不 Toast）：`/api/Auth/login`、`/api/Auth/refresh`、`/api/Captcha/create`。

