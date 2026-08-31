# Task I-6 Review: Phase I 文档锚点

**Reviewer:** Docs review subagent  
**Date:** 2026-08-30  
**Scope:** `doc/21-仿真器与联调闭环.md`（新文件）+ `Seven.Simulator/README.md` diff；路由对照 `SimulationController` / 关联 Controller

---

## Verdicts

| Dimension | Result |
|-----------|--------|
| **Spec compliance（路由 vs Controller）** | ✅ |
| **Task quality** | **Approved** |

---

## Diff reviewed

| File | Git state | Notes |
|------|-----------|-------|
| `doc/21-仿真器与联调闭环.md` | Untracked（全文新增） | 实施流程 + §5 API 速查 + Phase I 门禁 |
| `Seven.Simulator/README.md` | Modified | 文档索引、认证说明、API 表补全 reset/promote、验收清单 |

---

## Route verification

### `SimulationController` — `[Route("api/simulation")]`, `[AllowAnonymous]`

| Method | Doc/21 §5 | README | Controller | Match |
|--------|-----------|--------|------------|-------|
| POST | `/api/simulation/projects/validate-features` | ✅ | `[HttpPost("projects/validate-features")]` | ✅ |
| POST | `/api/simulation/deploy` | ✅ | `[HttpPost("deploy")]` | ✅ |
| POST | `/api/simulation/undeploy` | ✅ | `[HttpPost("undeploy")]` | ✅ |
| GET | `/api/simulation/deployments` | ✅ | `[HttpGet("deployments")]` | ✅ |
| POST | `/api/simulation/reset` | ✅ | `[HttpPost("reset")]` | ✅ |
| POST | `/api/simulation/promote-preview` | ✅ | `[HttpPost("promote-preview")]` | ✅ |
| POST | `/api/simulation/promote` | ✅ | `[HttpPost("promote")]` | ✅ |

**Request bodies（§5.1 / README）：** `ResetRequest(projectName)`、`UndeployRequest(projectName, removeLocations?)`、`SimPromoteRequest(projectName, devices[])` — 与 Controller record 定义一致。

### Related controllers（README / doc/21 引用）

| Method | Path | Controller | Match |
|--------|------|------------|-------|
| POST | `/api/Wcs/Triggers/destination-request` | `WcsTriggersController` `[Authorize]` | ✅ |
| POST | `/api/Wcs/Triggers/segment-feedback` | 同上 | ✅ |
| POST | `/api/WmsInboundOrder/add` | `WmsInboundOrdersController` `[Authorize]` | ✅ |
| POST | `/api/WmsInboundOrder/approve/{id}` | `[HttpPost("approve/{id:int}")]` | ✅ |

doc/21 §5 另列 Hub `/hubs/sim-wcs-proxy`（三期档 B）— 标注为未来能力，非 Phase I 路由门禁。

---

## Spec compliance checklist

| Requirement | Status |
|-------------|--------|
| Phase I API 表与 `SimulationController` 七路由一致 | ✅ |
| 移除 Reset/Promote「待实现/占位」表述 | ✅ |
| Phase I 门禁：Deploy → Trigger → Promote preview 拒环回 | ✅ §4 / README 验收项 |
| README API 表与 doc/21 §5 对齐 | ✅ |
| Trigger / 入库单标注 JWT | ✅ 与 `[Authorize]` 一致 |
| Simulation Deploy/Reset/Promote 标注 `[AllowAnonymous]` | ✅ |

---

## Findings

### Critical

*None.*

### Minor（非阻塞）

1. **环境准备不一致** — doc/21 §2 仍含 `dotnet ef database update`；README 开发节已删。建议后续统一，不影响路由 Spec。
2. **Trigger 路径粒度** — README 用 `/api/Wcs/Triggers/*` 通配；doc/21 写全路径。语义一致。
3. **doc/21 未入库** — 当前 untracked；合并前需 `git add`。

---

## Recommendation

**Approve Task I-6.** 文档路由与 Controller 完全对齐；无 Critical mismatch。Spec ✅。
