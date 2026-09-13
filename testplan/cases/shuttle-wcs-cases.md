# 四向穿梭车 WCS 详细测试用例

约定：[`../00-conventions.md`](../00-conventions.md) · 计划：[`../shuttle-wcs-e2e.md`](../shuttle-wcs-e2e.md)

---

## A. 分配与同层入库

### TC-FW-001 层→巷→位三阶段分配

| 字段 | 内容 |
|------|------|
| ID | TC-FW-001 |
| Priority | P0 |
| Features | FW |
| Automation | **DotNet** |
| Hook | `hook:wms.inbound.buildPallet` / SUDR |
| ExistingTest | `FourWayAllocatorTests` / `InboundToFourWayE2ETests` |

**前置**  
Fw_LayerPolicy / AislePolicy；Wms_Layer；空闲 Fw 货位。

**期望**  
AssignedLayer/Aisle/Location；Booking；轮转 Record。

**异常**  
MaxShuttleCount 满跳过巷；无空位失败。

---

### TC-FW-002 同层入库闭环（寻路+占边+回写）

| 字段 | 内容 |
|------|------|
| ID | TC-FW-002 |
| Priority | P0 |
| Automation | **DotNet** |
| Hook | `hook:wcs.trigger.segmentFeedback` |
| ExistingTest | `InboundToFourWayE2ETests` / `FourWayPathDispatcherTests` |

**步骤**  
组盘 → AcceptLeg → destination-request → 多段 feedback。

**期望**  
Path 推进；TrafficGuard 占/释；库存到 Fw.*。

**异常**  
占边失败保持 Routing 可重试；Cancel 释 `fw:flow:`。

---

### TC-FW-003 无地图降级单段

| 字段 | 内容 |
|------|------|
| ID | TC-FW-003 |
| Priority | P0 |
| Automation | **DotNet** |
| Hook | `hook:wcs.trigger.segmentFeedback` |
| ExistingTest | PathDispatcher 无边回退 |
| FutureTest | `FourWayPathDispatcherTests.NoMap_ShouldSingleSegment` |

---

### TC-FW-004 SUDR NG 本包拒收

| 字段 | 内容 |
|------|------|
| ID | TC-FW-004 |
| Priority | P0 |
| Type | Negative |
| Automation | **DotNet** |
| Hook | `hook:wcs.trigger.destinationRequest` |
| FutureTest | `FourWayDestinationServiceTests.NgCheck_ShouldReject` |

---

## B. 出库与停车

### TC-FW-010 同层出库停车账本状态机

| 字段 | 内容 |
|------|------|
| ID | TC-FW-010 |
| Priority | P0 |
| Automation | **DotNet** |
| Hook | `hook:wms.outbound.approve` |
| ExistingTest | `FourWayParkingTests` / `OutboundToFourWayE2ETests` |

**期望**  
Free→Reserved→（Running）Occupied；完成释 Free；Pri 门闩。

**异常**  
无空闲车位 → Suspended；有 Free 可唤醒。

---

### TC-FW-011 停车并发预订（仅一成功）

| 字段 | 内容 |
|------|------|
| ID | TC-FW-011 |
| Priority | P1 |
| Automation | **DotNet** |
| Hook | `hook:fw.parking.reserve` |
| ExistingTest | `FourWayParkingTests` 并发相关（若有） |
| FutureTest | `FourWayParkingTests.ConcurrentReserve_OnlyOneWins` |

---

### TC-FW-012 出库完成扣账

| 字段 | 内容 |
|------|------|
| ID | TC-FW-012 |
| Priority | P0 |
| Automation | **DotNet** |
| Hook | `hook:wcs.trigger.segmentFeedback` |
| ExistingTest | `OutboundToFourWayE2ETests` |
| 交叉 | TC-WMS-011 |

---

## C. 跨层提升

### TC-FW-020 单 Leg 三阶段顺序

| 字段 | 内容 |
|------|------|
| ID | TC-FW-020 |
| Priority | P0 |
| Automation | **DotNet** |
| Hook | `hook:wcs.trigger.segmentFeedback` |
| ExistingTest | `FourWayHoistE2ETests` |

**前置**  
两层货位 + HoistDevice + LayerPoint。

**期望**  
**仅 1 条** Bus Leg；ToSrcAp → HoistLift → FromDesEp；WMS 正确。

**异常**  
同口已 Dispatched → 后到 Suspended；Feedback≠OK 不盲目推进。

---

### TC-FW-021 同口排队唤醒

| 字段 | 内容 |
|------|------|
| ID | TC-FW-021 |
| Priority | P1 |
| Automation | **DotNet** |
| Hook | `hook:wcs.trigger.segmentFeedback` |
| ExistingTest | Hoist 联锁相关 |
| FutureTest | `FourWayHoistE2ETests.SameMouth_QueueThenWake` |

---

### TC-FW-022 跨层出库优先出库口

| 字段 | 内容 |
|------|------|
| ID | TC-FW-022 |
| Priority | P1 |
| Automation | **DotNet** |
| Hook | `hook:wms.outbound.approve` |
| FutureTest | `FourWayHoistOrchestratorTests.Outbound_PrefersOutboundAp` |

---

## D. 双包

### TC-FW-030 堆垛点 NG 四向静默

| 字段 | 内容 |
|------|------|
| ID | TC-FW-030 |
| Priority | P0 |
| Features | DUAL |
| Automation | **DotNet** |
| Hook | `hook:wcs.trigger.destinationRequest` |
| ExistingTest | DualPack 静默 |
| FutureTest | `FourWayDestinationServiceTests.ForeignStackerPointNg_ShouldSilent` |

---

### TC-FW-031 CanHandle 拒纯 Stk 两端

| 字段 | 内容 |
|------|------|
| ID | TC-FW-031 |
| Priority | P0 |
| Automation | **DotNet** |
| Hook | `hook:bus.acceptLeg` |
| ExistingTest | `FourWayPackTests.CanHandle_*` |
| 交叉 | TC-X-002 |

---

## E. 运维 Ops API（已落地，测试缺口）

### TC-OPS-FW-001 meta 与 canAcceptLegs

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-FW-001 |
| Priority | P0 |
| Automation | **ApiHttp**, DotNet |
| Hook | `hook:fw.ops.meta` |
| FutureTest | `FourWayOpsServiceTests.GetMeta_ShouldExposeGatewaysAndWcsFree` |

---

### TC-OPS-FW-002 board 活动集

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-FW-002 |
| Priority | P0 |
| Automation | **ApiHttp**, DotNet |
| Hook | `hook:fw.ops.board` |
| FutureTest | `FourWayOpsServiceTests.GetBoard_ShouldGroupTasks` |
| UiTestId | `fw-ops-monitor` |

---

### TC-OPS-FW-003 task-tree 折叠数据完整

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-FW-003 |
| Priority | P0 |
| Automation | **ApiHttp**, DotNet |
| Hook | `hook:fw.ops.taskTree` |
| FutureTest | `FourWayOpsServiceTests.GetTaskTree_ShouldIncludePathsAndHoist` |

---

### TC-OPS-FW-004 运维轻量入库（自动策略）

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-FW-004 |
| Priority | P0 |
| Automation | **ApiHttp**, DotNet, Browser |
| Hook | `hook:fw.ops.inbound` |
| FutureTest | `FourWayOpsServiceTests.Inbound_Auto_ShouldCreateTransport` |
| UiTestId | `fw-ops-inbound-submit` |

**前置**  
Gateway；容器类型；WCS Free/可接单；无未完成出库。

**期望**  
建 Bus（RefType=FourWayOpsInbound）；Container 落位链路可仿真完成。

---

### TC-OPS-FW-005 出入互斥（有出库禁入库）

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-FW-005 |
| Priority | P0 |
| Type | Gate |
| Automation | **ApiHttp**, DotNet |
| Hook | `hook:fw.ops.inbound` |
| FutureTest | `FourWayOpsServiceTests.Inbound_WhenOutboundActive_ShouldReject` |

**前置**  
存在未完成 FwRetrieval。

**期望**  
入库 API 返回互斥错误。

---

### TC-OPS-FW-006 指定货位不可选原因

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-FW-006 |
| Priority | P1 |
| Automation | **ApiHttp**, DotNet |
| Hook | `hook:fw.ops.pickableMap` |
| FutureTest | `FourWayOpsServiceTests.PickableMap_Occupied_ShouldBlock` |

**期望**  
occupied/booked/locked/handover → pickable=false + reason。

---

### TC-OPS-FW-007 指定点调度（WCS Free）

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-FW-007 |
| Priority | P0 |
| Automation | **ApiHttp**, DotNet, Browser |
| Hook | `hook:fw.ops.pointDispatch` |
| FutureTest | `FourWayOpsServiceTests.PointDispatch_WhenFree_ShouldDispatch` |
| UiTestId | `fw-ops-point-dispatch` |

**期望**  
成功建运并尽量 PathDispatch。

---

### TC-OPS-FW-008 指定点非 Free 拒批

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-FW-008 |
| Priority | P0 |
| Type | Gate |
| Automation | **ApiHttp**, DotNet |
| Hook | `hook:fw.ops.pointDispatch` |
| FutureTest | `FourWayOpsServiceTests.PointDispatch_WhenBusy_ShouldReject` |

---

### TC-OPS-FW-009 充电与结束充电

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-FW-009 |
| Priority | P1 |
| Automation | **ApiHttp**, DotNet, Browser |
| Hook | `hook:fw.ops.charge` / `hook:fw.ops.chargeStop` |
| FutureTest | `FourWayOpsServiceTests.Charge_StartAndStop` |
| UiTestId | `fw-ops-charge`, `fw-ops-charge-stop` |

**期望**  
非 Free 拒开充；stop 完成 FourWayCharge 类 Leg。

---

### TC-OPS-FW-010 强制完成 Shuttle

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-FW-010 |
| Priority | P0 |
| Automation | **ApiHttp**, DotNet, Browser |
| Hook | `hook:fw.ops.forceComplete` |
| FutureTest | `FourWayOpsServiceTests.ForceComplete_Shuttle_ShouldCompleteLeg` |
| UiTestId | `fw-ops-force-complete` |

---

### TC-OPS-FW-011 强制完成 Hoist / HoistExec

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-FW-011 |
| Priority | P1 |
| Automation | **ApiHttp**, DotNet |
| Hook | `hook:fw.ops.forceComplete` |
| FutureTest | `FourWayOpsServiceTests.ForceComplete_Hoist` |

---

### TC-OPS-FW-012 重发门控（Running 拒）

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-FW-012 |
| Priority | P0 |
| Type | Gate |
| Automation | **ApiHttp**, DotNet |
| Hook | `hook:fw.ops.resend` |
| FutureTest | `FourWayOpsServiceTests.Resend_WhenRunning_ShouldReject` |
| UiTestId | `fw-ops-resend` |

---

### TC-OPS-FW-013 联锁 scope=FourWay + 仓级急停

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-FW-013 |
| Priority | P0 |
| Automation | **ApiHttp**, DotNet, Browser |
| Hook | `hook:fw.ops.controlMode` |
| ExistingTest | `PlatformIfcCtlTests` |
| FutureTest | `FourWayOpsApiTests.ControlMode_EStop_Blocks` |
| UiTestId | `ops-estop-pack`, `ops-estop-global` |

---

## F. 运维 UI

### TC-OPS-FW-020 五页菜单与无入库表单在监控

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-FW-020 |
| Priority | P0 |
| Type | UI |
| Automation | **Browser**, Manual |
| Hook | `hook:ui.menu.fwOpsVisible` |
| FutureTest | `e2e/ops-menu.spec.ts` |
| UiTestId | `rail-menu-fw-ops`, `fw-ops-monitor` |

**期望**  
监控/入库/穿梭/提升/联锁存在；监控页无入库表单；无执行运维。

---

### TC-OPS-FW-021 穿梭页四段布局（手动默认折叠）

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-FW-021 |
| Priority | P1 |
| Automation | **Browser** |
| Hook | `hook:ui.fw.ops.shuttleLayout` |
| FutureTest | `e2e/wcs-ops-shuttle.spec.ts` |

**期望**  
常用/任务展开；手动控制 `<details>` 默认关闭。

---

### TC-OPS-FW-022 入库页策略切换与可选货位对话框

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-FW-022 |
| Priority | P1 |
| Automation | **Browser** |
| Hook | `hook:ui.fw.ops.inboundForm` |
| UiTestId | `fw-ops-inbound-submit` |

---

### TC-OPS-FW-023 任务树轮询保持（3～5s）

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-FW-023 |
| Priority | P2 |
| Automation | **Browser** |
| Hook | `hook:ui.fw.ops.taskTreePoll` |

**期望**  
刷新后选中/展示不异常闪断（实现允许 JSON 重绘；后续可测折叠态保留）。

---

## 索引

| ID | 标题 | P | Automation |
|----|------|---|------------|
| TC-FW-001～004 | 分配/入库/拒收 | P0 | DotNet |
| TC-FW-010～012 | 出库停车 | P0–P1 | DotNet |
| TC-FW-020～022 | 提升 | P0–P1 | DotNet |
| TC-FW-030～031 | 双包 | P0 | DotNet |
| TC-OPS-FW-001～013 | Ops API | P0–P1 | ApiHttp/DotNet |
| TC-OPS-FW-020～023 | Ops UI | P0–P2 | Browser |
