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
