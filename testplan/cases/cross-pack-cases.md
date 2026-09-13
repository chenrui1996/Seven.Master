# 跨包与运维 IA 详细测试用例

约定：[`../00-conventions.md`](../00-conventions.md) · 计划：[`../cross-pack-closed-loop.md`](../cross-pack-closed-loop.md)

---

## A. 主数据与包边界

### TC-X-001 一仓多包前缀隔离

| 字段 | 内容 |
|------|------|
| ID | TC-X-001 |
| Priority | P0 |
| Features | DUAL |
| Automation | **DotNet** |
| Hook | `hook:wms.location.save` |
| ExistingTest | `PackPrefixAndMultiPackTests` |

**步骤**  
同仓建 `Stk.*` 与 `Fw.*` 货位/巷道/层。

**期望**  
PackId 与前缀一致；错前缀/缺前缀保存失败。

---

### TC-X-002 CanHandle 拒串包

| 字段 | 内容 |
|------|------|
| ID | TC-X-002 |
| Priority | P0 |
| Automation | **DotNet** |
| Hook | `hook:bus.acceptLeg` |
| ExistingTest | `FourWayPackTests` / `StackerPackTests` CanHandle |

**步骤**  
纯 Stk 两端 → FourWay；纯 Fw 两端 → Stacker。

**期望**  
`CanHandle=false`；无错误包内任务。

---

### TC-X-003 HandoverLink 跨包两段

| 字段 | 内容 |
|------|------|
| ID | TC-X-003 |
| Priority | P0 |
| Automation | **DotNet** |
| Hook | `hook:bus.order.create` |
| ExistingTest | `OrchestrationBusTests.TwoPacks_WithHandover_*` |

**前置**  
`Wms_HandoverLink` FromPack→ToPack + LocationCode。

**期望**  
第一段完成 → 第二段接单；库存最终目标包；交接位占用符合设计。

**异常**  
Link 缺失 → 拒建或可诊断失败。

---

### TC-X-004 Trigger 静默矩阵

| 字段 | 内容 |
|------|------|
| ID | TC-X-004 |
| Priority | P0 |
| Type | Matrix |
| Automation | **DotNet** |
| Hook | `hook:wcs.trigger.destinationRequest` |
| ExistingTest | DualPack / Destination 静默 |
| FutureTest | `DualPackTriggerSilenceTests.Matrix_AllThree` |

| # | 条件 | 期望 |
|---|------|------|
| 1 | 四向点 + NG | 堆垛静默 |
| 2 | 堆垛点 + NG | 四向静默 |
| 3 | 本包点 + NG | 本包 Reject |

---

## B. 运维菜单 IA（已落地）

### TC-X-010 仅 stacker：立库运维可见

| 字段 | 内容 |
|------|------|
| ID | TC-X-010 |
| Priority | P0 |
| Type | UI |
| Features | STK |
| Automation | **Browser**, Manual |
| Hook | `hook:ui.menu.stkOpsVisible` |
| FutureTest | `e2e/ops-menu.spec.ts` `@stk-only` |
| UiTestId | `rail-menu-stk-ops` |

**期望可见**  
立库运维四页、堆垛仿真。  

**期望不可见**  
FwOps*、执行运维、`WcsOpsFolder`、顶级 Scada 作为运维入口。

---

### TC-X-011 仅 fourway：四向运维可见

| 字段 | 内容 |
|------|------|
| ID | TC-X-011 |
| Priority | P0 |
| Type | UI |
| Features | FW |
| Automation | **Browser**, Manual |
| Hook | `hook:ui.menu.fwOpsVisible` |
| FutureTest | `e2e/ops-menu.spec.ts` `@fw-only` |
| UiTestId | `rail-menu-fw-ops` |

**期望不可见**  
StkOps*、执行运维。

---

### TC-X-012 Dual：双边运维 + 无执行运维

| 字段 | 内容 |
|------|------|
| ID | TC-X-012 |
| Priority | P0 |
| Features | DUAL |
| Automation | **Browser**, Manual |
| Hook | `hook:ui.menu.noWcsOpsFolder` |
| UiTestId | `rail-menu-wcsops`（断言不存在） |

**额外**  
- MainLayout 不因运维钉轨底。  
- `/Scada/Floor2d` 显示迁移提示或 Features 隐藏。  
- 接口日志在系统管理（交叉 TC-WMS-041）。

---

### TC-X-013 Features API 与菜单过滤一致

| 字段 | 内容 |
|------|------|
| ID | TC-X-013 |
| Priority | P1 |
| Automation | **ApiHttp**, Browser |
| Hook | `hook:cfg.features` + `hook:sys.menu` |
| FutureTest | `FeatureOptionsWcsTests` + e2e features toggle |

**步骤**  
拉取 `/api/config/features`；切换 packs 后重新 getMenu（或重登）。

**期望**  
前端过滤与 flags 一致。

---

## C. 联锁与监控

### TC-X-020 仓级急停双边生效

| 字段 | 内容 |
|------|------|
| ID | TC-X-020 |
| Priority | P0 |
| Features | DUAL |
| Automation | **ApiHttp**, DotNet, Browser |
| Hook | `hook:fw.ops.controlMode` / `hook:stk.ops.controlMode` |
| ExistingTest | `PlatformIfcCtlTests` |
| FutureTest | `ControlModeCrossPackTests.GlobalEStop_BlocksBoth` |
| UiTestId | `ops-estop-global` |

**步骤**  
四向运维开仓级急停 → 立库读到急停 → 两侧尝试接单。

**期望**  
双边拒派；解除后恢复。

---

### TC-X-021 包专属模式互不影响

| 字段 | 内容 |
|------|------|
| ID | TC-X-021 |
| Priority | P1 |
| Automation | **ApiHttp**, DotNet |
| Hook | `hook:ctl.mode.set` |
| FutureTest | `ControlModeCrossPackTests.PackMode_Isolated` |

**步骤**  
FourWay=Manual，Stacker=Auto。

**期望**  
仅四向拒接；堆垛仍可接（按 CanAcceptLegs 实现）。

---

### TC-X-022 监控任务板 Pack 过滤

| 字段 | 内容 |
|------|------|
| ID | TC-X-022 |
| Priority | P1 |
| Type | UI |
| Automation | **Browser**, ApiHttp |
| Hook | `hook:stk.ops.board` / `hook:fw.ops.board` |
| UiTestId | `stk-ops-monitor`, `fw-ops-monitor` |

**期望**  
Stk board 无 Fw 任务；Fw board 无 Stk 任务（按 API 数据源）。

---

## D. 黄金冒烟剧本

### TC-X-030 剧本 A — 立库入库账本闭环

| 字段 | 内容 |
|------|------|
| ID | TC-X-030 |
| Priority | P0 |
| Automation | **DotNet**（已有）, Browser（预留） |
| Hook | `hook:smoke.stacker.inbound` |
| ExistingTest | `InboundToStackerE2ETests` |
| 交叉 | TC-WMS-002/004 + TC-SRM-010 |

---

### TC-X-031 剧本 B — 四向入库账本闭环

| 字段 | 内容 |
|------|------|
| ID | TC-X-031 |
| Priority | P0 |
| Automation | **DotNet**, Browser |
| Hook | `hook:smoke.fourway.inbound` |
| ExistingTest | `InboundToFourWayE2ETests` |
| 交叉 | TC-WMS-003/004 + TC-FW-002 |

---

### TC-X-032 剧本 C — 四向跨层出库

| 字段 | 内容 |
|------|------|
| ID | TC-X-032 |
| Priority | P0 |
| Automation | **DotNet** |
| Hook | `hook:smoke.fourway.crossLayerOutbound` |
| ExistingTest | `FourWayHoistE2ETests` + Outbound |
| 交叉 | TC-FW-020 + TC-WMS-011 |

---

### TC-X-033 剧本 D — 运维指定点

| 字段 | 内容 |
|------|------|
| ID | TC-X-033 |
| Priority | P0 |
| Automation | **ApiHttp**, Browser |
| Hook | `hook:fw.ops.pointDispatch` |
| FutureTest | `FourWayOpsServiceTests` + e2e |
| 交叉 | TC-OPS-FW-007 |

---

### TC-X-034 剧本 E — 运维出入互斥

| 字段 | 内容 |
|------|------|
| ID | TC-X-034 |
| Priority | P0 |
| Automation | **ApiHttp**, DotNet |
| Hook | `hook:fw.ops.inbound` |
| FutureTest | `FourWayOpsServiceTests.Inbound_WhenOutboundActive_ShouldReject` |
| 交叉 | TC-OPS-FW-005 |

---

### TC-X-035 Simulator 黄金路径录制（可选）

| 字段 | 内容 |
|------|------|
| ID | TC-X-035 |
| Priority | P2 |
| Automation | **Manual**, ApiHttp |
| Hook | `hook:sim.deploy.promote` |
| API | `/api/simulation/deploy` 等 |

**说明**  
发布前人工走一遍 Simulator Promote + 剧本 A/B，作为现场冒烟附件。

---

## E. 种子与退役回归

### TC-X-040 DbSeeder 退役 WcsOpsFolder

| 字段 | 内容 |
|------|------|
| ID | TC-X-040 |
| Priority | P0 |
| Automation | **DotNet**, Manual |
| Hook | `hook:sys.menu` |
| FutureTest | `DbSeederOpsMenuTests.Retire_WcsOpsFolder_Disabled` |

**步骤**  
启动应用跑种子 → 查库 `Sys_Menus`。

**期望**  
`WcsOpsFolder`/`CtlMode`/`ScadaFolder` Enable=0；存在 StkOps*/FwOps*；IfcApiLog 父=系统管理。

---

### TC-X-041 权限码 Search 可进运维页

| 字段 | 内容 |
|------|------|
| ID | TC-X-041 |
| Priority | P1 |
| Automation | **ApiHttp**, Browser |
| Hook | `hook:sys.menu` |
| FutureTest | `RoleAuthOpsMenuTests.Admin_HasFwOpsSearch` |

**期望**  
管理员具备 `FwOpsMonitor.Search` 等；路由不 403 回首页。

---

## 索引

| ID | 标题 | P | Automation |
|----|------|---|------------|
| TC-X-001～004 | 多包边界 | P0 | DotNet |
| TC-X-010～013 | 菜单 IA | P0–P1 | Browser |
| TC-X-020～022 | 联锁/监控 | P0–P1 | ApiHttp/Browser |
| TC-X-030～035 | 冒烟剧本 | P0–P2 | DotNet/ApiHttp/Manual |
| TC-X-040～041 | 种子权限 | P0–P1 | DotNet/ApiHttp |
