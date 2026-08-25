# Review Package — Task 3
## Stat
 ...\347\253\257\345\274\200\345\217\221\346\214\207\345\215\227.md" | 6 +++---
 1 file changed, 3 insertions(+), 3 deletions(-)

## Diff
diff --git "a/doc/03-\345\211\215\347\253\257\345\274\200\345\217\221\346\214\207\345\215\227.md" "b/doc/03-\345\211\215\347\253\257\345\274\200\345\217\221\346\214\207\345\215\227.md"
index 8c225b5..8e96198 100644
--- "a/doc/03-\345\211\215\347\253\257\345\274\200\345\217\221\346\214\207\345\215\227.md"
+++ "b/doc/03-\345\211\215\347\253\257\345\274\200\345\217\221\346\214\207\345\215\227.md"
@@ -115,25 +115,25 @@ Login.vue
 Pinia persist **只**保存：`token`、`refreshToken`、`userId`、`userName`、`userTrueName`、`roleId`。
 
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
 
 非 401 的 HTTP 错误默认只 `reject`，由调用方处理。

