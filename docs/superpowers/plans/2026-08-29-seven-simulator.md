# Seven.Simulator 联调闭环 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 simulation-spa 能力按 Vue3 重写迁入 `Seven.Simulator`，落地「编辑地图 → Deploy 主数据 → Gateway/Trigger 仿真 → Promote 生产」四期闭环。

**Architecture:** 独立 SPA `Seven.Simulator` + 同进程 `Seven.WebApi` 的 `/api/simulation/*`；一期走 `IEquipmentTriggerPort`；三期加 TCP Gateway Hub；生产走 Promote + DeviceComm。包表按 `packId` 隔离写入。

**Tech Stack:** Vue 3.5 · Vite 8 · TS · Pinia · Element Plus · axios · SignalR；后端 .NET 8 · EF Core；三期+ `three`。

**Spec:** [`docs/superpowers/specs/2026-08-29-seven-simulator-design.md`](../specs/2026-08-29-seven-simulator-design.md)  
**实施说明（给人看）:** [`doc/21-仿真器与联调闭环.md`](../../../doc/21-仿真器与联调闭环.md)

## Global Constraints

- 不引用 RCS4Shuttle / LES 程序集；只迁契约与能力。
- 仓码前缀固定 `SIM_`；未 Promote 禁止真机 IP 冒充生产。
- `Features.Simulator` 只控前端入口；Deploy API 始终可用。
- 库位权威仅 `Wms_Location`；包表用 Code 引用。
- 一期不加 three.js。
- 用户未要求时不要 `git commit`（本计划中的 Commit 步骤改为「可选：待用户要求再提交」）。

---

## File map（按职责）

| 路径 | 职责 |
|------|------|
| `Seven.Simulator/src/stores/project.ts` | 工程模型与导入导出 |
| `Seven.Simulator/src/lib/project/schema.ts` | 类型与默认值 |
| `Seven.Simulator/src/lib/project/simproj-adapter.ts` | `.simproj` → `.sevenproj` |
| `Seven.Simulator/src/views/MapEditor.vue` | 一期简易编辑；二期挂 Canvas |
| `Seven.Simulator/src/views/Player.vue` | Trigger / 入库快捷 / 后期 3D |
| `Seven.Simulator/src/views/Promote.vue` | 真机表单 + Promote |
| `Seven.Application/Simulator/*.cs` | DTO 与接口 |
| `Seven.Infrastructure/Simulator/SimulationDeployService.cs` | Deploy/Undeploy/Reset/Promote |
| `Seven.WebApi/Controllers/Simulator/SimulationController.cs` | HTTP |
| `Seven.Tests/Simulator/*` | 单测 |
| `Seven.Infrastructure/Simulator/Gateway/*` | 三期 TCP+Hub |

---

# Phase I — 闭环可演示（先做完再进 II）

### Task I-1: 扩展工程 schema 与 DTO

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
- [ ] **Step 5:** 可选提交（待用户要求）。

### Task I-2: Deploy 增强 — 边 / 申请点 / Scd

**Files:**
- Modify: `Seven.Infrastructure/Simulator/SimulationDeployService.cs`
- Test: `Seven.Tests/Simulator/SimulationDeployServiceTests.cs`

**Interfaces:**
- Consumes: `SimProjectDto.Map.Edges` / `RequestPoints` / `Scada`
- Produces: `StkRoute` 或 `FwNode`+`FwRoute`；`StkRequestPoint`/`FwRequestPoint`；`ScdView`+`ScdNodeBind`

- [ ] **Step 1:** 在现有测试中增加用例：`packId=stacker` + 2 nodes + 1 edge + 1 requestPoint → Deploy 后 DB 有对应 `Stk_Route`/`Stk_RequestPoint`。
- [ ] **Step 2:** 跑测试确认失败（功能未写）。
- [ ] **Step 3:** 实现 Deploy 分支：edges→路由；requestPoints→申请点；scada.views 空则按 nodes 自动生成默认 `Scd_View`「SIM_{name}」。
- [ ] **Step 4:** fourway 最小用例：nodes→`FwNode`（已有则增强），edges→`FwRoute`。
- [ ] **Step 5:** 全量 `SimulationDeployServiceTests` 通过。

### Task I-3: Reset API

**Files:**
- Modify: `ISimulationDeployService` + `SimulationDeployService` + `SimulationController`
- Test: `SimulationDeployServiceTests`

- [ ] **Step 1:** 定义 `ResetAsync(projectName, ct)`：取消/关闭该 `SIM_` 仓相关未完成 `Bus_TransportOrder` / 包任务（按现有状态枚举设为取消或完成失败），不清 Location。
- [ ] **Step 2:** `POST /api/simulation/reset` body `{ projectName }`。
- [ ] **Step 3:** 测试：Deploy 后造一条运输单 stub（若测试基础设施允许）或仅断言方法不抛且 Deployment 状态不变。
- [ ] **Step 4:** 测试通过。

### Task I-4: Promote / promote-preview API

**Files:**
- Modify: Application DTO + DeployService + Controller
- Test: Promote 单测

**Interfaces:**
- Request: `{ projectName, devices: [{ code, host, port, protocol }], apply: bool }`
- Preview: 返回将修改的连接列表；拒绝 host 为 `127.0.0.1`/`localhost`
- Apply: 写入 `CommConnection`（若 DeviceComm 表存在）或 `Sim_Deployment` 扩展字段 + 工程 promote 快照；设置 runtime 标记；**不**自动改仓码（文档约定：生产仓需单独建或后续 rename 任务）

- [ ] **Step 1:** 失败用例：host=127.0.0.1 → 400/业务错误。
- [ ] **Step 2:** preview 返回 devices 清单。
- [ ] **Step 3:** apply 写入配置并记 `Sim_Deployment` 备注 `Promoted`。
- [ ] **Step 4:** 测试通过。

### Task I-5: Simulator UI — Map / Player / Promote 接通一期 API

**Files:**
- Modify: `Seven.Simulator/src/views/MapEditor.vue`、`Player.vue`、`Promote.vue`、`Features.vue`
- Modify: `http` 封装如需 JWT（Triggers 需登录时：Simulator 登录页或复用 token localStorage）

- [ ] **Step 1:** MapEditor：支持编辑 edges（from/to 下拉节点）、requestPoints 表单；Deploy/Undeploy 保持。
- [ ] **Step 2:** Player：保留 Trigger；增加「快捷入库」调用 `/api/WmsInboundOrder/add`+`approve`（需 token）；增加 Reset 按钮。
- [ ] **Step 3:** Promote：表格绑定 `project.promote.devices`；调用 preview/promote；禁止保存 127.0.0.1。
- [ ] **Step 4:** 手工验收清单写入 `Seven.Simulator/README.md`。
- [ ] **Step 5:** `npm run build` 于 `Seven.Simulator` 通过。

### Task I-6: Phase I 文档锚点

**Files:**
- Verify: `doc/21-仿真器与联调闭环.md` Phase I 章节与代码一致
- Modify: `Seven.Simulator/README.md` API 表补 Reset/Promote

- [ ] **Step 1:** 对照 README 与 Controller 路由一致。
- [ ] **Step 2:** 本地跑通：WebApi + Simulator → Deploy → Trigger → Promote preview。

**Phase I 完成门禁：** 堆垛 Demo 工程可 Deploy；Trigger 返回成功；Promote preview 拒绝环回地址。

---

# Phase II — 完整 2D 编辑器

### Task II-1: Canvas 渲染内核

**Files:**
- Create: `Seven.Simulator/src/components/map/MapCanvas.vue`、`useMapCamera.ts`、`hitTest.ts`
- Reference behavior: `simulation-spa/src/editor/canvas-renderer.js`（重写，不复制粘贴巨石）

- [ ] **Step 1:** 用 canvas 绘制 nodes（圆/方）与 edges；支持平移缩放。
- [ ] **Step 2:** 点击选中节点；属性面板改 code/x/y。
- [ ] **Step 3:** 单元测试：`hitTest` 纯函数（vitest 或现有 node 测试风格）。

### Task II-2: 设备库与连线

**Files:**
- Create: `deviceCatalog.ts`（首期子集：SRM、CoordPoint、Conveyor、RequestPoint）
- Modify: MapEditor 集成拖放与 `connections`

- [ ] **Step 1:** 从面板拖设备到画布 → `map.devices`。
- [ ] **Step 2:** 端口连线写入 `map.connections`（`from: "dev.port"`）。
- [ ] **Step 3:** Deploy 适配：connection 编译为 edges 或 requestPoints（明确规则写在 schema 注释）。

### Task III-3 → 编号 Task II-3: Space 编译与拓扑校验

**Files:**
- Create: `lib/map/spaceCompiler.ts`、`topologyValidator.ts`（逻辑迁自 device-space-compiler / topology-validator，TS 重写）
- Test: `Seven.Simulator` 下 `*.test.ts` 或仓库 scripts

- [ ] **Step 1:** 校验失败时 MapEditor 顶部展示错误列表，阻止 Deploy。
- [ ] **Step 2:** 校验通过才启用 Deploy 按钮。

**Phase II 门禁：** 手动画简易立库（≥1 SRM + ≥3 location + 连线）Deploy 成功。

---

# Phase III — 3D Player + Gateway 保真

### Task III-1: 引入 three 与最小场景

**Files:**
- Modify: `Seven.Simulator/package.json` 增加 `three`
- Create: `components/player/ThreeScene.vue`、`SimPlayerView.vue`

- [ ] **Step 1:** 根据 `map.nodes/devices` 生成简单 mesh（箱体/货架占位即可）。
- [ ] **Step 2:** OrbitControls；与 Player 路由集成，可用 feature flag 开关 3D。

### Task III-2: SignalR WCS Proxy Hub（后端）

**Files:**
- Create: `Seven.Infrastructure/Simulator/Gateway/SimWcsProxyHub.cs`、`SimGatewayHostedService.cs`
- Modify: `Program.cs` / DI 注册；仅当配置 `Simulator:Gateway:Enabled=true` 时启动 TCP

- [ ] **Step 1:** Hub 方法 `SendWcsMessage` / 事件 `OnWcsMessageReceived`（对齐 RCS 契约名，便于迁前端）。
- [ ] **Step 2:** TCP 监听 `wcsConnections[].serverPort`；环回 echo/Emulator 回复。
- [ ] **Step 3:** 集成测试或手工：Simulator 连 Hub 收发一条报文。

### Task III-3: 前端 Gateway 客户端 + Emulator 动画钩子

**Files:**
- Create: `lib/comms/simWcsProxy.ts`、`emulators/` 子集
- Modify: Player 增加 `simCommsMode=Gateway` 面板

- [ ] **Step 1:** Start Gateway → 连 Hub → 发送测试报文。
- [ ] **Step 2:** 收到任务类报文时移动 3D 占位小车（最小动画）。

**Phase III 门禁：** Gateway 模式下能 Start、收发报文、3D 有可见反馈。

---

# Phase IV — 导入与运维增强

### Task IV-1: `.simproj.json` 适配器

**Files:**
- Create: `lib/project/simproj-adapter.ts`
- Test: 用 `simulation-spa/Demo/*.simproj.json` 一份最小样例（复制到 `Seven.Simulator/fixtures/`）

- [ ] **Step 1:** 映射 meta/map.devices/connections → sevenproj。
- [ ] **Step 2:** 不支持的设备类型进 `warnings[]`，不阻断导入。

### Task IV-2: Excel 导入 API

**Files:**
- Create: Controller action `import-shuttle-excel`（逻辑参考 RCS，输出 sevenproj JSON）
- 前端：MapEditor 上传入口

- [ ] **Step 1:** 上传 xlsx → 返回 nodes 列表。
- [ ] **Step 2:** 前端合并进工程后可 Deploy。

### Task IV-3: 路径组预览与调度看板（最小）

**Files:**
- API: `POST /api/simulation/route-groups/preview`
- UI: Player 侧栏展示 preview 结果

- [ ] **Step 1:** fourway 环检测返回 suggested groups（可先启发式）。
- [ ] **Step 2:** 文档更新 `doc/21` Phase IV 验收步骤。

**Phase IV 门禁：** 一份 RCS Demo JSON 导入 → 警告可接受 → Deploy 成功。

---

## 执行顺序与依赖

```text
I-1 → I-2 → I-3 → I-4 → I-5 → I-6
         ↓
        II-1 → II-2 → II-3
         ↓
        III-1 → III-2 → III-3
         ↓
        IV-1 → IV-2 → IV-3
```

每期门禁通过后再开下一期。推荐 **subagent-driven-development** 按 Task 派工。

---

## Spec coverage（自检）

| Spec 要求 | Task |
|-----------|------|
| 编辑地图 | I-5, II-* |
| Deploy 主数据 | I-2 |
| Trigger 仿真 | I-5 |
| Gateway 保真 | III-2, III-3 |
| Promote 真机 | I-4, I-5 |
| simproj 兼容 | IV-1 |
| Excel / 路径组 | IV-2, IV-3 |
| 文档实施流程 | I-6 + doc/21 |

无 TBD 占位；Commit 步骤服从用户「未要求不提交」约束。
