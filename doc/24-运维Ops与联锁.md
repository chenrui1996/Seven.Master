# 24 — 运维 Ops API 与菜单联锁

> 与设计稿 [`design/ops/01-ops-menu-restructure.md`](../design/ops/01-ops-menu-restructure.md) 对齐；本文记录 **2026-09-13 活库实测** 后的实现契约，供前后端与测试共用。

## 1. 菜单信息架构

| 位置 | TableName | 说明 |
|------|-----------|------|
| 立库WCS → 运维 | `StkOpsFolder` | 子：Monitor / Srm / RequestPoint / CtlMode |
| 四向车WCS → 运维 | `FwOpsFolder` | 子：Monitor / Inbound / Shuttle / Hoist / CtlMode |
| 系统管理 | `IfcApiLog` | 接口日志（**不在**执行运维下） |
| 已退役 | 原「执行运维」 | `enable=0`，`getMenu` 不可见；`getMenuList` 仍可见禁用行 |

前端 Features 过滤：`stores/features.ts` 隐藏遗留 Ops；包开关关闭时隐藏对应 `StkOps*` / `FwOps*`。

Vue 路由示例：

- `/Wcs/Stacker/Ops/Monitor` … `ControlMode`
- `/Wcs/FourWay/Ops/Monitor` … `Inbound` / `Shuttle` / `Hoist` / `ControlMode`
- `/Platform/InterfaceLog`

## 2. HTTP 路由

### 四向 ` /api/Wcs/FourWay/Ops `

| Method | Path | 权限 |
|--------|------|------|
| GET | `meta` | FwOpsInbound.Search |
| GET | `board` | FwOpsMonitor.Search |
| GET | `task-tree` | FwOpsShuttle.Search |
| POST | `inbound` | FwOpsInbound.Add |
| GET | `inbound/pickable-map` | FwOpsInbound.Search |
| POST | `point-dispatch` | FwOpsShuttle.Update |
| POST | `charge` / `charge/stop` | FwOpsShuttle.Update |
| POST | `force-complete` | FwOpsShuttle.Update |
| POST | `resend` | FwOpsShuttle.Update |
| GET/POST | `control-mode` | FwOpsCtlMode.Search / Update |

### 立库 ` /api/Wcs/Stacker/Ops `

| Method | Path | 权限 |
|--------|------|------|
| GET | `board` / `task-tree` | StkOpsMonitor / StkOpsSrm |
| POST | `force-complete` / `resend` | StkOpsSrm.Update |
| GET | `request-points` | StkOpsRequest.Search |
| POST | `request-point/{id}/enable\|disable` | StkOpsRequest.Update |
| GET/POST | `control-mode` | StkOpsCtlMode.* |

均需 `[Authorize]`；无 Token → **401**。

## 3. 关键契约（易错）

### 3.1 `control-mode`

```json
{ "mode": 1, "eStop": false, "globalEStop": false }
```

- `mode`：`WcsControlMode` **数值**（0=Auto，1=Manual，…）。字符串名会 400。
- FourWay POST 可写 `globalEStop`（仓级急停）；Stacker 对称。

### 3.2 `force-complete`

```json
{ "targetType": "shuttle", "id": "00000000-0000-0000-0000-000000000000" }
```

| 包 | targetType |
|----|------------|
| FourWay | `putAway` \| `retrieval` \| `shuttle` \| `hoist` \| `hoistExec` \| `path` |
| Stacker | `putAway` \| `retrieval` \| `device` |

缺字段 → ModelState 400；未知类型 / 无实体 → 业务 `status=false`。

## 4. 验证码与自动化

- `Features.Captcha=true` 时登录页展示 `.captcha-code`（当前为可读字符）。
- `GET /api/Captcha/create` 同步返回 `key`+`code`，ApiHttp/Playwright 可直接使用。
- 生产若改为纯图片挑战，需同步改 [`testplan/scripts/run-api-cases.ps1`](../testplan/scripts/run-api-cases.ps1) 与 e2e。

## 5. 本地回归命令

见 [`testplan/TEST-REPORT-2026-09-13.md`](../testplan/TEST-REPORT-2026-09-13.md) §6。

实现类：`FourWayOpsService` / `StackerOpsService`；控制器：`WcsOpsControllers.cs`。
