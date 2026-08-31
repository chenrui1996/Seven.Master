# Task I-2 Review: Deploy 增强 — 边 / 申请点 / Scada

**Reviewer:** Code review subagent  
**Date:** 2026-08-29  
**Scope:** Spec compliance + code quality (read-only; diff + source verified)

---

## Verdicts

| Dimension | Result |
|-----------|--------|
| **Spec compliance** | ✅ |
| **Task quality** | **Approved** |

---

## Spec Compliance Checklist

| Requirement | Status | Notes |
|-------------|--------|-------|
| Step 1: stacker + 2 nodes + 1 edge + 1 requestPoint → `Stk_Route` / `Stk_RequestPoint` | ✅ | `Deploy_Stacker_WithEdgeAndRequestPoint_ShouldCreateRouteAndRequestPoint` |
| Step 2: RED then GREEN | ✅ | Report 记录 4 pass / 2 fail → 6 pass；测试与实现顺序一致 |
| Step 3: edges→路由；requestPoints→申请点；scada.views 空→默认 `Scd_View`「SIM_{name}」+ NodeBind | ✅ | `EnsureStackerSeedAsync` + `EnsureScadaAsync` |
| Step 4: fourway nodes→`FwNode`；edges→`FwRoute` | ✅ | 既有 `EnsureFourWayMapAsync`；新增回归测试 |
| Step 5: 全量 `SimulationDeployServiceTests` 通过 | ✅ | 本地重跑：`6 passed, 0 failed` |
| null 集合当空处理 | ✅ | `Edges ?? []`、`RequestPoints ?? []`、`Scada?.Views` |
| `Wms_Location` 权威 | ✅ | 库位仍由 Deploy 主循环写入；`ScdNodeBind.LocationCode` 无 FK（与 Domain 注释一致） |
| 无 LES 引用 | ✅ | 变更文件内无 LES |
| 禁止 git commit | ✅ | working tree only |

### 产出物对齐

| DTO 输入 | 产出实体 | 实现 |
|----------|----------|------|
| `Map.Edges` | `StkRoute` / `FwRoute` | stacker：`MapCode=""` 默认图；fourway：挂当前 `FwMapVersion` |
| `Map.RequestPoints` | `StkRequestPoint` | 非空用 DTO；空/null 回退 nodes 种子（向后兼容） |
| `Scada` null 或 `Views` 空 | `ScdView` + `ScdNodeBind` | `SIM_{SanitizeCode(name)}`；画布 min 800×600 |
| `DeployResult.EdgeCount` | — | stacker / fourway 均回填 edge 计数 |

---

## Findings

### Critical

*None.*

### Important

*None.*

### Minor

1. **幂等性未测** — stacker route、request point、scada bind 均有 duplicate skip，但无 re-deploy 用例。实现合理，建议后续补一条即可。

2. **edge 字段 Trim 不一致** — stacker 对 `edge.From/To` 调用 `.Trim()`，fourway 沿用既有逻辑未 Trim。与 pre-existing 风格一致，非本任务引入的回归风险。

3. **显式 `scada.views` 为 no-op** — 符合 brief 范围声明；后续任务接入前无测试覆盖（可接受）。

4. **`EnsureScadaAsync` 对所有 packId 执行** — brief 未限定 pack；fourway 部署也会生成 SCADA 视图，行为合理且与「仿真工程默认可视化」一致。

---

## Code Quality Notes

**Strengths**

- 扩展点清晰：`EnsureStackerSeedAsync` 先 edges 再 requestPoints，保留 nodes 回退与 assignment policy 种子。
- null-as-empty 与 I-1 反序列化语义衔接正确；`SampleProject` 无 requestPoints 时原测试仍通过（2 个 `StkRequestPoint`）。
- SCADA 默认视图复用已有 `SanitizeCode`；画布尺寸自节点 bounds 推导，空图 fallback 800×600。
- 幂等：route 按 `(MapCode, FromCode, ToCode)`；bind 按 `(ViewId, LocationCode)`。
- TDD 证据充分：3 个新 Fact，命名与断言贴合 brief。

**Constraints verified**

- 变更文件：`SimulationDeployService.cs`、`SimulationDeployServiceTests.cs`（`Seven.Net8` 下）。
- 未改 DTO 默认值；未引入 LES；未 commit。

---

## Recommendation

**Approve Task I-2.** 实现满足 brief 全部步骤与全局约束；6/6 测试通过。Minor 项为可选跟进，不构成返工条件。

**Suggested follow-ups (later tasks):**

- 显式 `scada.views` / `promote.devices` Deploy。
- Re-deploy 幂等集成测试。
- 可选：fourway `FwRequestPoint` DTO 映射。
