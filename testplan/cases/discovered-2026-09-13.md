# 测试过程新发现用例（2026-09-13）

执行活库回归时补充；已纳入索引。

---

### TC-AUTH-API-001 Health 探针

| 字段 | 内容 |
|------|------|
| ID | TC-AUTH-API-001 |
| Priority | P0 |
| Automation | **ApiHttp** |
| Hook | `hook:sys.health` |
| ExistingTest | `HealthApiTests`（工厂） |
| API | `GET /api/Health` |

**期望** `status=true`，message 含运行中提示。

---

### TC-AUTH-API-002 Captcha 创建

| 字段 | 内容 |
|------|------|
| ID | TC-AUTH-API-002 |
| Priority | P0 |
| Automation | **ApiHttp** |
| Hook | `hook:sys.captcha` |
| API | `GET /api/Captcha/create` |

**期望** 返回 `data.key` + `data.code`（当前实现为明文验证码，便于自动化；生产若改为纯图片需同步改脚本）。

**安全注意** 明文 code 仅适合开发/联调；若对外暴露需改为仅图片或一次性挑战。

---

### TC-AUTH-API-003 登录（验证码）

| 字段 | 内容 |
|------|------|
| ID | TC-AUTH-API-003 |
| Priority | P0 |
| Automation | **ApiHttp**, Browser |
| Hook | `hook:sys.login` |
| ExistingTest | `AuthApiTests.Login_WithValidCredentials_ShouldReturnToken`（工厂常关 captcha） |
| API | `POST /api/Auth/login` |

**步骤** create captcha → login 带 `verificationCode`+`uuid`。  
**期望** `data.token` / `refreshToken` / `permissions[]`。

---

### TC-AUTH-UI-001 浏览器登录

| 字段 | 内容 |
|------|------|
| ID | TC-AUTH-UI-001 |
| Priority | P0 |
| Automation | **Browser** |
| Hook | `hook:ui.login` |
| UiTestId | `login-submit`（待补 data-testid） |
| FutureTest | `e2e/specs/ops-ui.spec.ts`（已实现） |

**期望** 读 `.captcha-code` 填表 → 离开 `/login`。

---

### TC-OPS-API-MODE-ENUM control-mode 数值枚举

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-API-MODE-ENUM |
| Priority | P0 |
| Type | Contract |
| Automation | **ApiHttp** |
| Hook | `hook:fw.ops.controlMode` |
| API | `POST /api/Wcs/FourWay/Ops/control-mode` 与 Stacker 对称 |

**期望**

- GET `data.mode` 为 number（`WcsControlMode`）。
- POST 接受 `{"mode":0|1|...}`；**拒绝** `"Manual"` 字符串（400）。
- 同 Body 可设 `eStop` / `globalEStop`。

---

### TC-OPS-API-FORCE-SHAPE force-complete 字段

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-API-FORCE-SHAPE |
| Priority | P0 |
| Type | Contract |
| Automation | **ApiHttp** |
| Hook | `hook:fw.ops.forceComplete` / `hook:stk.ops.forceComplete` |

**期望** Body：

```json
{ "targetType": "shuttle|hoist|hoistExec|putAway|retrieval|path|device", "id": "<guid>" }
```

无任务时业务 `status=false`；缺 `targetType` → ASP.NET 校验 400。

---

### TC-OPS-STK-006b 申请点启停往返

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-STK-006b |
| Priority | P1 |
| Automation | **ApiHttp** |
| Hook | `hook:stk.ops.requestPoint` |
| 依赖 | 至少 1 条申请点 |

**步骤** disable → enable。  
**空库** → SKIP；落库轮已种子 `RETAIN.RP-01` → PASS。

---

### TC-BUG-WMS-JSON-CYCLE 单据 add 响应循环引用

| 字段 | 内容 |
|------|------|
| ID | TC-BUG-WMS-JSON-CYCLE |
| Priority | P0 |
| Type | Regression |
| Automation | **ApiHttp** |
| Hook | `hook:wms.inbound.add` |

**现象**  
`POST .../add` 实体已入库，序列化 `Lines.Order.Lines...` 环 → HTTP 500。

**修复**  
`Program.cs`：`ReferenceHandler.IgnoreCycles`；行实体 `[JsonIgnore]`；脚本 `Create-OrderKeep` 按单号回查。重启 WebApi 后 add 应直接 200。
