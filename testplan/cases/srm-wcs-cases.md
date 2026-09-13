# 立库（堆垛）WCS 详细测试用例

约定：[`../00-conventions.md`](../00-conventions.md) · 计划：[`../srm-wcs-e2e.md`](../srm-wcs-e2e.md)

---

## A. 分配与申请

### TC-SRM-001 SelectAisle 权重与记录

| 字段 | 内容 |
|------|------|
| ID | TC-SRM-001 |
| Priority | P0 |
| Type | HappyPath |
| Features | STK |
| Automation | **DotNet** |
| Hook | `hook:wcs.trigger.destinationRequest` |
| ExistingTest | `StackerPackTests` / Allocator 相关；Inbound E2E |
| FutureTest | `StackerAisleAllocatorTests.SelectAisle_PrefersHigherWeight` |

**前置**  
多巷策略；MinEmptySlots 可满足；仓未盘点锁。

**步骤**  
1. 组盘异址建 PutAway。  
2. `destination-request`，申请点类型 AisleRequest，CheckResult=OK。

**期望**  
PutAway=`AisleAssigned`；Ep 正确；AssignmentRecord 写入；高权重优先。

**异常**  
全巷不可用 → Reject/PutAway Failed；盘点锁仓跳过该仓巷道。

---

### TC-SRM-002 LocationRequest 双深 Booking

| 字段 | 内容 |
|------|------|
| ID | TC-SRM-002 |
| Priority | P0 |
| Automation | **DotNet** |
| Hook | `hook:wcs.trigger.destinationRequest` |
| ExistingTest | `StackerDoubleDeepTests` |
| FutureTest | — |

**前置**  
`Stk_LocationProfile` 双深组；浅深数据齐。

**步骤**  
LocationRequest 分配。

**期望**  
目标 IsBooked；不产生孤二深；可 Dispatch(Bin)。

**异常**  
仅剩孤深候选 → 不选该位。

---

### TC-SRM-003 CheckResult≠OK 拒收

| 字段 | 内容 |
|------|------|
| ID | TC-SRM-003 |
| Priority | P0 |
| Type | Negative |
| Automation | **DotNet** |
| Hook | `hook:wcs.trigger.destinationRequest` |
| ExistingTest | Pack/Destination 拒收相关 |
| FutureTest | `StackerDestinationServiceTests.NgCheck_ShouldReject` |

**期望**  
拒收；PutAway Failed 或等价；不错误占巷。

---

## B. 寻路与段

### TC-SRM-010 多段拆腿顺序下发

| 字段 | 内容 |
|------|------|
| ID | TC-SRM-010 |
| Priority | P0 |
| Automation | **DotNet** |
| Hook | `hook:wcs.trigger.segmentFeedback` |
| ExistingTest | `StackerPathDispatcherTests` / Inbound E2E |

**前置**  
`Stk_Route` 有边；容量可测。

**步骤**  
1. 分配后生成多条 DeviceTask。  
2. 断言仅首段 Dispatched。  
3. 逐段 Feedback OK。

**期望**  
释流后下一段 Dispatched；末段完成 → PutAway Completed → Bus → WMS。

**异常**  
满容边 → 无路或单测 Router 失败路径；Cancel 释尽 RouteFlow。

---

### TC-SRM-011 无路网降级单段

| 字段 | 内容 |
|------|------|
| ID | TC-SRM-011 |
| Priority | P0 |
| Automation | **DotNet** |
| Hook | `hook:wcs.trigger.segmentFeedback` |
| ExistingTest | PathDispatcher 无边回退 |
| FutureTest | `StackerPathDispatcherTests.NoRoute_ShouldSingleSegment` |

**期望**  
单条 DeviceTask From→To 仍可完成闭环。

---

### TC-SRM-012 段反馈 NG 不推进

| 字段 | 内容 |
|------|------|
| ID | TC-SRM-012 |
| Priority | P1 |
| Type | Negative |
| Automation | **DotNet** |
| Hook | `hook:wcs.trigger.segmentFeedback` |
| FutureTest | `StackerDestinationServiceTests.FeedbackNg_ShouldNotAdvance` |

---

## C. 出库与干涉

### TC-SRM-020 同组 Pri 门闩

| 字段 | 内容 |
|------|------|
| ID | TC-SRM-020 |
| Priority | P0 |
| Automation | **DotNet** |
| Hook | `hook:wms.outbound.approve` |
| ExistingTest | `OutboundToStackerE2ETests` / Retrieval Pri |

**前置**  
同组 Pri=1,2 两条 Retrieval。

**期望**  
仅 Pri=1 先派；Pri=2 Suspended；1 完成后 2 Dispatched。

---

### TC-SRM-021 深浅干涉 StackerTransfer

| 字段 | 内容 |
|------|------|
| ID | TC-SRM-021 |
| Priority | P0 |
| Automation | **DotNet** |
| Hook | `hook:wms.outbound.approve` |
| ExistingTest | `StackerDoubleDeepTests` / DepthGuard 相关 |
| FutureTest | `StackerDepthGuardTests.DeepBlocked_ShouldCreateTransfer` |

**前置**  
深位出库，同组浅位有货。

**期望**  
深位 Suspended；Bus `StackerTransfer` 浅→空位；完成后深位可派。

**异常**  
无同巷空位 → Suspended/失败策略符合实现说明。

---

### TC-SRM-022 BlockingPoint 重分配

| 字段 | 内容 |
|------|------|
| ID | TC-SRM-022 |
| Priority | P1 |
| Automation | **DotNet** |
| Hook | `hook:wcs.trigger.destinationRequest` |
| FutureTest | `StackerDestinationServiceTests.BlockingPoint_ShouldReallocate` |

**期望**  
在途段作废、预约释放、重选；旧 DeviceTask 不占边。

---

## D. 双包交互

### TC-SRM-030 四向点外形 NG 堆垛静默

| 字段 | 内容 |
|------|------|
| ID | TC-SRM-030 |
| Priority | P0 |
| Features | DUAL |
| Automation | **DotNet** |
| Hook | `hook:wcs.trigger.destinationRequest` |
| ExistingTest | DualPack / Destination 静默相关 |
| FutureTest | `StackerDestinationServiceTests.ForeignFourWayPointNg_ShouldSilent` |

**期望**  
堆垛不误 Reject。

---

## E. 运维 Ops（已落地，测试缺口）

### TC-OPS-STK-001 board 返回活动任务

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-STK-001 |
| Priority | P0 |
| Type | HappyPath |
| Automation | **ApiHttp**, DotNet |
| Hook | `hook:stk.ops.board` |
| ExistingTest | — |
| FutureTest | `StackerOpsServiceTests.GetBoard_ShouldListActiveTasks` |
| UiTestId | `stk-ops-monitor` |
| API | `GET /api/Wcs/Stacker/Ops/board` |

**期望**  
putAways/retrievals/devices 结构完整；summary.wcsFree 布尔正确。

---

### TC-OPS-STK-002 task-tree 按 device/putAway

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-STK-002 |
| Priority | P0 |
| Automation | **ApiHttp**, DotNet |
| Hook | `hook:stk.ops.taskTree` |
| FutureTest | `StackerOpsServiceTests.GetTaskTree_ByDevice_ShouldNest` |

---

### TC-OPS-STK-003 强制完成 DeviceTask

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-STK-003 |
| Priority | P0 |
| Type | Gate |
| Automation | **ApiHttp**, DotNet, Browser |
| Hook | `hook:stk.ops.forceComplete` |
| FutureTest | `StackerOpsServiceTests.ForceComplete_Device_ShouldCompleteLegWhenLast` |
| UiTestId | `stk-ops-force-complete` |

**前置**  
有未完成 DeviceTask；可选急停关。

**步骤**  
`POST force-complete` targetType=device。

**期望**  
段 Completed；若无剩余段则 Bus Complete / WMS 推进。

**异常**  
无权限 → 403；错误 Id → 业务错误。

---

### TC-OPS-STK-004 强制完成 PutAway

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-STK-004 |
| Priority | P0 |
| Automation | **ApiHttp**, DotNet |
| Hook | `hook:stk.ops.forceComplete` |
| FutureTest | `StackerOpsServiceTests.ForceComplete_PutAway_ShouldComplete` |

---

### TC-OPS-STK-005 重发门控（Dispatched 拒）

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-STK-005 |
| Priority | P0 |
| Type | Gate |
| Automation | **ApiHttp**, DotNet |
| Hook | `hook:stk.ops.resend` |
| FutureTest | `StackerOpsServiceTests.Resend_WhenDispatched_ShouldReject` |
| UiTestId | `stk-ops-resend` |

**期望**  
status=Dispatched 时拒绝；Created/可重发态成功并再次 DispatchMove。

---

### TC-OPS-STK-006 申请点临时停用

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-STK-006 |
| Priority | P1 |
| Automation | **ApiHttp**, Browser |
| Hook | `hook:stk.ops.requestPointEnable` |
| FutureTest | `StackerOpsServiceTests.DisableRequestPoint_ShouldPersist` |
| UiTestId | `stk-ops-request-disable` |

**期望**  
IsEnabled=false；后续本点 SUDR 不接或按实现拒。

---

### TC-OPS-STK-007 联锁急停拒派（包 scope）

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-STK-007 |
| Priority | P0 |
| Automation | **ApiHttp**, DotNet, Browser |
| Hook | `hook:stk.ops.controlMode` |
| ExistingTest | `PlatformIfcCtlTests` EStop 拒接（平台） |
| FutureTest | `StackerOpsApiTests.EStop_ShouldBlockAccept` |
| UiTestId | `ops-estop-pack` |

**步骤**  
POST control-mode eStop=true → 尝试 AcceptLeg/组盘建运。

**期望**  
拒接/拒派；解除后恢复。

---

### TC-OPS-STK-008 运维四页菜单可见（UI）

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-STK-008 |
| Priority | P0 |
| Type | UI |
| Features | STK |
| Automation | **Browser**, Manual |
| Hook | `hook:ui.menu.stkOpsVisible` |
| FutureTest | `e2e/ops-menu.spec.ts` |
| UiTestId | `rail-menu-stk-ops` |

**期望**  
监控/堆垛运维/申请点/联锁可见；无执行运维；无顶级 Scada 为唯一入口。

---

### TC-OPS-STK-009 监控页打开仿真与运输单链接

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-STK-009 |
| Priority | P2 |
| Automation | **Browser** |
| Hook | `hook:ui.stk.ops.monitorLinks` |
| UiTestId | `stk-ops-monitor` |

---

### TC-OPS-STK-010 Trigger 页 destination + segment（手工仿真）

| 字段 | 内容 |
|------|------|
| ID | TC-OPS-STK-010 |
| Priority | P1 |
| Automation | **Browser**, ApiHttp |
| Hook | `hook:wcs.trigger.destinationRequest` |
| UiTestId | `wcs-trigger-dest-submit`, `wcs-trigger-seg-submit` |
| FutureTest | `e2e/stacker-trigger.spec.ts` |

---

## 索引

| ID | 标题 | P | Automation |
|----|------|---|------------|
| TC-SRM-001～003 | 分配/拒收 | P0 | DotNet |
| TC-SRM-010～012 | 寻路/段 | P0–P1 | DotNet |
| TC-SRM-020～022 | 出库/Blocking | P0–P1 | DotNet |
| TC-SRM-030 | 静默 | P0 | DotNet |
| TC-OPS-STK-001～007 | Ops API | P0–P1 | ApiHttp/DotNet |
| TC-OPS-STK-008～010 | Ops UI | P0–P2 | Browser |
