# Task I-2: Deploy 增强 — 边 / 申请点 / Scd

**Files:**
- Modify: `Seven.Infrastructure/Simulator/SimulationDeployService.cs`
- Test: `Seven.Tests/Simulator/SimulationDeployServiceTests.cs`

**Interfaces:**
- Consumes: `SimProjectDto.Map.Edges` / `RequestPoints` / `Scada` (null → treat as empty)
- Produces: `StkRoute` 或 `FwNode`+`FwRoute`；`StkRequestPoint`/`FwRequestPoint`；`ScdView`+`ScdNodeBind`

- [ ] **Step 1:** 测试：`packId=stacker` + 2 nodes + 1 edge + 1 requestPoint → Deploy 后有 `Stk_Route` / `Stk_RequestPoint`
- [ ] **Step 2:** RED then GREEN
- [ ] **Step 3:** 实现：edges→路由；requestPoints→申请点；scada.views 空则按 nodes 自动生成默认 `Scd_View`「SIM_{name}」+ NodeBind
- [ ] **Step 4:** fourway 最小：nodes→FwNode；edges→FwRoute
- [ ] **Step 5:** 全量 SimulationDeployServiceTests 通过
- [ ] **禁止 git commit**

## Prior task note
DTO optional collections deserialize as **null** when absent — Deploy must use `?? Array.Empty` / empty list. Do not change DTO defaults unless needed.

## Existing code
Read current `SimulationDeployService.cs` — it already seeds stacker request points from devices sometimes and fourway map from nodes. Extend carefully; follow existing EF patterns and entity property names in Domain.

Work root: `d:\Junheinrich\Junheinrich.Master\Seven.Master`
