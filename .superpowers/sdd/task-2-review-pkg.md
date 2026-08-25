# Review Package — Task 2 (after fix)
## Stat
 Seven.Vue3/src/api/http.ts | 59 +++++++++++++++++++++++++++++++++-------------
 1 file changed, 43 insertions(+), 16 deletions(-)

## Diff
diff --git a/Seven.Vue3/src/api/http.ts b/Seven.Vue3/src/api/http.ts
index efcf9a8..2995449 100644
--- a/Seven.Vue3/src/api/http.ts
+++ b/Seven.Vue3/src/api/http.ts
@@ -1,89 +1,116 @@
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
 
 const http = axios.create({
   baseURL: import.meta.env.VITE_API_BASE_URL,
   timeout: 30000
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
 
 export default http
 

