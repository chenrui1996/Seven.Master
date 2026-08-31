# Task I-1: 扩展工程 schema 与 DTO

**Files:**
- Modify: `Seven.Simulator/src/stores/project.ts`
- Create: `Seven.Simulator/src/lib/project/schema.ts`
- Modify: `Seven.Application/Simulator/`（`SimProjectDto` 及相关）
- Test: `Seven.Tests/Simulator/SimProjectDtoTests.cs`（可新建）

**Interfaces:**
- Produces: `SimProject` 含 `requestPoints`、`scada`、`promote`、`meta.simCommsMode`

- [ ] **Step 1:** 在 `schema.ts` 定义与 spec §3 对齐的 TypeScript 接口，并从 `project.ts` 引用。
- [ ] **Step 2:** 扩展后端 `SimProjectDto` / `SimMapDto`：增加 `RequestPoints`、`Scada`、`PromoteDevices`；`SimFeaturesDto` 保持兼容。
- [ ] **Step 3:** 写测试：反序列化最小 JSON（仅 nodes）仍成功；含 requestPoints 时字段非空。
- [ ] **Step 4:** `dotnet test --filter FullyQualifiedName~SimProject` 通过。
- [ ] **Step 5:** 可选提交（待用户要求）— **本任务禁止 git commit**。

## Spec §3 工程模型要点（ verbatim 对齐）

工程需支持：
- `meta.simCommsMode`: `"Trigger" | "Gateway"`
- `map.requestPoints`: `{ code, mappedLocationCode }[]`
- `map` 可保留现有 nodes/edges/devices；可增加 canvas/layers（可选一期）
- `scada.views`: 可空数组
- `promote.devices`: `{ code, host, port, protocol }[]`

后端 DTO 当前在 `Seven.Application/Simulator/ISimulationDeployService.cs`（record 形式）。扩展时保持 **最小 JSON（仅 nodes）仍可反序列化**（缺省集合用空列表）。

工作目录：`d:\Junheinrich\Junheinrich.Master\Seven.Master`
