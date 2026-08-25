# Task 1 Report: 增加 `login.expired` 三语文案

## Status

**DONE**

## Scope

为 Token 失效跳转登录页功能预备 i18n 键 `login.expired`（嵌套于 `login` 对象下的 `expired` 字段）。本任务仅修改 locale JSON，不涉及 `http.ts` 或其它文件。

## Changes

| 文件 | 变更 |
|------|------|
| `Seven.Vue3/src/locales/lang/zh-CN.json` | 在 `login.requestFailed` 后新增 `login.expired` |
| `Seven.Vue3/src/locales/lang/en-US.json` | 同上 |
| `Seven.Vue3/src/locales/lang/ja-JP.json` | 同上 |

### 文案（与设计文档 / 任务简报一致）

| 语言 | Key 路径 | 值 |
|------|----------|-----|
| zh-CN | `login.expired` | 登录已过期，请重新登录 |
| en-US | `login.expired` | Session expired. Please sign in again |
| ja-JP | `login.expired` | ログインの有効期限が切れました。再度ログインしてください |

插入位置：各语言 `login` 对象末尾，`requestFailed` 条目之后，与简报 Step 1–3 一致。

## Verification

在 `Seven.Vue3` 目录执行：

```powershell
node -e "JSON.parse(require('fs').readFileSync('src/locales/lang/zh-CN.json','utf8')); JSON.parse(require('fs').readFileSync('src/locales/lang/en-US.json','utf8')); JSON.parse(require('fs').readFileSync('src/locales/lang/ja-JP.json','utf8')); console.log('ok')"
```

**结果：** 退出码 0，输出 `ok`，无 SyntaxError。

## Self-Review

- [x] 仅修改任务指定的三个 JSON 文件
- [x] 未修改 `http.ts`、文档或其它文件
- [x] 三语字符串与 `docs/superpowers/specs/2026-08-05-token-expired-redirect-login-design.md` 及任务简报一致
- [x] JSON 语法合法（逗号、引号、UTF-8 日文无转义问题）
- [x] 键名 `expired` 位于 `login` 对象内，运行时等价于 `i18n.t('login.expired')`

## Commits

无（按用户规则未提交）。

## Concerns

- 任务简报文件 `task-1-brief.md` 本地编码显示为乱码；实现以设计规格与现有 locale 中 `requestFailed` 上下文为准，文案已与设计文档表格核对一致。
- 本任务未做运行时 UI 验证；`login.expired` 将在后续任务接入 `http.ts` 后生效。

## Next Steps (out of scope)

后续任务应在 `forceLogoutToLogin()` 中使用 `ElMessage.error(i18n.t('login.expired'))`。
