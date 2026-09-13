# WMS 详细测试用例

约定见 [`../00-conventions.md`](../00-conventions.md)。计划见 [`../wms-e2e.md`](../wms-e2e.md)。

---

## A. 入库

### TC-WMS-001 同址组盘直接完成（无运输）

| 字段 | 内容 |
|------|------|
| ID | TC-WMS-001 |
| Priority | P0 |
| Type | HappyPath |
| Features | WMS |
| Automation | **DotNet**, ApiHttp |
| Hook | `hook:wms.inbound.buildPallet` |
| ExistingTest | `WmsOrderServiceTests` 同址/无运输相关 Fact（若无则 Future） |
| FutureTest | `WmsOrderServiceTests.BuildPallet_SameLocation_ShouldCompleteWithoutBus` |
| UiTestId | `wms-inbound-build-pallet` |
| API | `POST .../buildPallet/{id}` |

**前置**  
仓库启用 WMS；收货位=目标位（或同址平库）；容器类型存在。

**步骤**  
1. `add` 入库单（物料、数量>0）。  
2. `approve`。  
3. `buildPallet`：收货位=目标位，容器号唯一。

**期望**  
- Detail=`Completed`；Order 可至 Completed。  
- Stock 在该位；**无** `Bus_TransportOrder`。  
- Location 占用/容器 LocationCode 一致。

**异常/边界**  
- qty≤0 → 拒批，无 Stock。  
- 容器号冲突 → 明确错误，无半成品单据态错乱。

**数据清理**  
删除本用例订单/库存或使用隔离 DB。

---

### TC-WMS-002 异址组盘建运（stacker）

| 字段 | 内容 |
|------|------|
| ID | TC-WMS-002 |
| Priority | P0 |
| Type | HappyPath |
| Features | STK |
| Automation | **DotNet** |
| Hook | `hook:wms.inbound.buildPallet` |
| ExistingTest | `InboundToStackerE2ETests.BuildPallet_Allocate_ThenStackerSimulate_ShouldPutStockAtStkLocation`（及同类） |
| FutureTest | — |
| UiTestId | `wms-inbound-build-pallet` |
| API | buildPallet + Bus |

**前置**  
`EnabledPackIds` 含 stacker；策略/巷道/货位 `Stk.*`；Allocator 可用。

**步骤**  
1. 建单审核。  
2. buildPallet（收货≠目标或允许自动分配）。  
3. 断言 Bus Leg `PackId=stacker`，`Stk_PutAwayTask` 存在。

**期望**  
Detail=`Transporting`；Stock 暂在收货位；目标位 Booking（若分配）。

**异常**  
无可分配货位 → 组盘失败；不残留错误 Booking。

---

### TC-WMS-003 异址组盘建运（fourway）

| 字段 | 内容 |
|------|------|
| ID | TC-WMS-003 |
| Priority | P0 |
| Type | HappyPath |
| Features | FW |
| Automation | **DotNet** |
| Hook | `hook:wms.inbound.buildPallet` |
| ExistingTest | `InboundToFourWayE2ETests.*` |
| FutureTest | — |
| UiTestId | `wms-inbound-build-pallet` |

同 TC-WMS-002，期望 `PackId=fourway`、`Fw_PutAwayTask`/`Fw_ShuttleTask`。

---

### TC-WMS-004 运输完成后库存落到目标位

| 字段 | 内容 |
|------|------|
| ID | TC-WMS-004 |
| Priority | P0 |
| Type | HappyPath |
| Features | STK 或 FW |
| Automation | **DotNet** |
| Hook | `hook:wcs.trigger.segmentFeedback` |
| ExistingTest | `InboundToStackerE2ETests` / `InboundToFourWayE2ETests` 完整闭环方法 |
| FutureTest | `WmsHttpE2ETests.Inbound_TriggerFeedback_ShouldCompleteStock` |
| API | Triggers destination-request + segment-feedback 循环 |

**前置**  
TC-WMS-002 或 003 已建运。

**步骤**  
1. 按包模拟 SUDR（如需要）与全部段 Feedback=OK。  
2. 等待/调用至 Leg Completed。

**期望**  
- PutAway Completed；Container 在目标 `Stk.*`/`Fw.*`。  
- Detail/Order 完成；Ledger 可查（若启用）。  
- 源收货位释放占用（按产品规则）。

**异常**  
中途 Feedback≠OK → 不落错误位；Detail 保持 Transporting。

---

### TC-WMS-005 运输完成幂等（重复 Complete）

| 字段 | 内容 |
|------|------|
| ID | TC-WMS-005 |
| Priority | P0 |
| Type | Negative / Regression |
| Features | STK 或 FW |
| Automation | **DotNet** |
| Hook | `hook:bus.leg.completeIdempotent` |
| ExistingTest | —（若无明确 Fact） |
| FutureTest | `OrchestrationBusTests.CompleteLeg_Twice_ShouldNotDoubleStock` |

**步骤**  
闭环完成后再次对同一 Leg 发 Completed 事件。

**期望**  
Stock 数量不变；无第二笔错误 Ledger；不抛未处理异常（或安全幂等返回）。

---

### TC-WMS-006 入库审核前数量非法

| 字段 | 内容 |
|------|------|
| ID | TC-WMS-006 |
| Priority | P1 |
| Type | Negative |
| Automation | **DotNet**, ApiHttp |
| Hook | `hook:wms.inbound.add` |
| FutureTest | `WmsOrderServiceTests.Create_InvalidQty_ShouldReject` |

**期望**  
拒建或拒审；无 Stock。

---

## B. 出库

### TC-WMS-010 审核预留 AvailableQty 并建运

| 字段 | 内容 |
|------|------|
| ID | TC-WMS-010 |
| Priority | P0 |
| Type | HappyPath |
| Features | STK 或 FW |
| Automation | **DotNet** |
| Hook | `hook:wms.outbound.approve` |
| ExistingTest | `OutboundToStackerE2ETests` / `OutboundToFourWayE2ETests` |
| UiTestId | `wms-outbound-approve` |

**前置**  
库存 Available 充足；From≠To；WcsGroupNo/WcsPri 已设。

**步骤**  
1. 建出库单 → approve。  

**期望**  
- AvailableQty 减少（Book）。  
- PickingTask 生成。  
- Bus Retrieval 路径创建（NeedsTransport）。

**异常**  
库存不足 → 拒审，无 Bus。

---

### TC-WMS-011 出库运输完成扣账

| 字段 | 内容 |
|------|------|
| ID | TC-WMS-011 |
| Priority | P0 |
| Type | HappyPath |
| Automation | **DotNet** |
| Hook | `hook:wcs.trigger.segmentFeedback` |
| ExistingTest | `OutboundTo*E2ETests.*ShouldMoveStock*` 类方法 |

**期望**  
Stock Ship；源位释放；出库/拣选完成。

**异常**  
Feedback NG → 不扣账。

---

### TC-WMS-012 平库直发 Ship（无运输）

| 字段 | 内容 |
|------|------|
| ID | TC-WMS-012 |
| Priority | P0 |
| Type | HappyPath |
| Features | WMS |
| Automation | **DotNet**, ApiHttp |
| Hook | `hook:wms.outbound.ship` |
| ExistingTest | `WmsOrderServiceTests` 直发相关 |
| UiTestId | `wms-outbound-ship` |

**期望**  
直接扣账；无 Bus。

---

### TC-WMS-013 运输中禁止手动 Ship

| 字段 | 内容 |
|------|------|
| ID | TC-WMS-013 |
| Priority | P0 |
| Type | Gate |
| Automation | **DotNet**, ApiHttp |
| Hook | `hook:wms.outbound.ship` |
| FutureTest | `WmsOrderServiceTests.Ship_WhileTransporting_ShouldReject` |

**前置**  
出库已挂运未完成。

**期望**  
Ship 失败，提示运输中；库存不变。

---

### TC-WMS-014 同组 Pri 滚动（出库侧断言）

| 字段 | 内容 |
|------|------|
| ID | TC-WMS-014 |
| Priority | P1 |
| Type | HappyPath |
| Features | STK 或 FW |
| Automation | **DotNet** |
| Hook | `hook:wms.outbound.approve` |
| ExistingTest | 包侧 Pri 测试 + Outbound E2E |
| 交叉 | TC-SRM-005 / TC-FW-004 |

---

## C. 盘点

### TC-WMS-020 盘点 Diff>0 调增

| 字段 | 内容 |
|------|------|
| ID | TC-WMS-020 |
| Priority | P0 |
| Type | HappyPath |
| Automation | **DotNet** |
| Hook | `hook:wms.cyclecount.confirm` |
| ExistingTest | `WmsOrderServiceTests.CycleCount_ConfirmAdjust_*` |
| UiTestId | `wms-cyclecount-confirm` |

**步骤**  
CreatePlan → RecordCount（实盘>账面）→ ConfirmAdjust。

**期望**  
Receive 增加；单 Completed；Location/Stock 一致。

---

### TC-WMS-021 盘点 Diff<0 调减

| 字段 | 内容 |
|------|------|
| ID | TC-WMS-021 |
| Priority | P0 |
| Type | HappyPath |
| Automation | **DotNet** |
| Hook | `hook:wms.cyclecount.confirm` |
| ExistingTest | 同上系列 |

---

### TC-WMS-022 未全部盘点禁止确认

| 字段 | 内容 |
|------|------|
| ID | TC-WMS-022 |
| Priority | P0 |
| Type | Gate |
| Automation | **DotNet**, ApiHttp |
| Hook | `hook:wms.cyclecount.confirm` |
| FutureTest | `WmsOrderServiceTests.CycleCount_Confirm_WhenIncomplete_ShouldReject` |

**期望**  
确认失败；库存不变。

---

### TC-WMS-023 盘点锁仓影响立库分配（联运）

| 字段 | 内容 |
|------|------|
| ID | TC-WMS-023 |
| Priority | P1 |
| Type | Gate |
| Features | STK |
| Automation | **DotNet** |
| Hook | `hook:wms.cyclecount.add` + 分配 |
| FutureTest | `StackerAisleAllocatorTests.Skip_WhenWarehouseCycleCountLocked` |
| 交叉 | TC-SRM-001 异常 |

---

## D. PDA

### TC-WMS-030 PDA 菜单与收货冒烟

| 字段 | 内容 |
|------|------|
| ID | TC-WMS-030 |
| Priority | P1 |
| Type | HappyPath |
| Automation | **DotNet**, ApiHttp |
| Hook | `hook:wms.pda.menu` / `hook:wms.pda.inbound` |
| ExistingTest | `PdaServiceTests` |

**期望**  
JWT 有效返回菜单；收货与 Web 账本一致。

**异常**  
无 Token → 401。

---

### TC-WMS-031 PDA 上架确认

| 字段 | 内容 |
|------|------|
| ID | TC-WMS-031 |
| Priority | P1 |
| Automation | **DotNet** |
| Hook | `hook:wms.pda.putaway` |
| ExistingTest | `PdaServiceTests` 上架相关 |

---

### TC-WMS-032 PDA 盘点录数

| 字段 | 内容 |
|------|------|
| ID | TC-WMS-032 |
| Priority | P1 |
| Automation | **DotNet** |
| Hook | `hook:wms.pda.cyclecount` |
| ExistingTest | `PdaServiceTests.CycleCount_RecordByLocation_*` |

**说明**  
拣选/发货 PDA **已知未做**，不阻断 P0 门禁。

---

## E. 接口日志与权限

### TC-WMS-040 非法外部请求写 Ifc 日志

| 字段 | 内容 |
|------|------|
| ID | TC-WMS-040 |
| Priority | P2 |
| Type | Regression |
| Automation | **DotNet**, ApiHttp |
| Hook | `hook:ifc.log.page` |
| ExistingTest | `PlatformIfcCtlTests`（日志写入） |
| FutureTest | `PlatformIfcCtlTests.Reject_ShouldPersistApiLog` |

---

### TC-WMS-041 接口日志菜单在系统管理（UI）

| 字段 | 内容 |
|------|------|
| ID | TC-WMS-041 |
| Priority | P0 |
| Type | UI |
| Automation | **Browser**, Manual |
| Hook | `hook:ui.menu.ifcUnderSystem` |
| ExistingTest | — |
| FutureTest | `e2e/ops-menu.spec.ts` |
| UiTestId | 系统管理树下 InterfaceLog；负向 `rail-menu-wcsops` |

**步骤**  
1. Features 开 orchestrationBus 或 wms。  
2. 登录查看菜单树。

**期望**  
「接口日志」父级为系统管理；**不**在「执行运维」。

---

## F. Web 运维快捷页（Browser 预留）

### TC-WMS-050 入库快捷页审核+组盘

| 字段 | 内容 |
|------|------|
| ID | TC-WMS-050 |
| Priority | P1 |
| Type | UI |
| Automation | **Browser** |
| Hook | `hook:ui.wms.inboundOps` |
| UiTestId | `wms-inbound-approve`, `wms-inbound-build-pallet` |
| FutureTest | `e2e/wms-inbound-ops.spec.ts` |

**步骤**  
打开 `/Wms/InboundOrder` → 建单 → 审核 → 组盘。

**期望**  
与 API 一致的状态展示；错误 Toast 可读。

---

### TC-WMS-051 出库快捷页

| 字段 | 内容 |
|------|------|
| ID | TC-WMS-051 |
| Priority | P1 |
| Automation | **Browser** |
| Hook | `hook:ui.wms.outboundOps` |
| UiTestId | `wms-outbound-approve`, `wms-outbound-ship` |

---

## 用例索引速查

| ID | 标题 | P | Automation |
|----|------|---|------------|
| TC-WMS-001 | 同址组盘 | P0 | DotNet, ApiHttp |
| TC-WMS-002 | 异址建运 STK | P0 | DotNet |
| TC-WMS-003 | 异址建运 FW | P0 | DotNet |
| TC-WMS-004 | 回写落账 | P0 | DotNet |
| TC-WMS-005 | Complete 幂等 | P0 | DotNet |
| TC-WMS-006 | 非法数量 | P1 | DotNet, ApiHttp |
| TC-WMS-010 | 出库审核建运 | P0 | DotNet |
| TC-WMS-011 | 出库完成扣账 | P0 | DotNet |
| TC-WMS-012 | 平库直发 | P0 | DotNet, ApiHttp |
| TC-WMS-013 | 运输中禁 Ship | P0 | DotNet, ApiHttp |
| TC-WMS-014 | Pri 滚动 | P1 | DotNet |
| TC-WMS-020～022 | 盘点 | P0 | DotNet |
| TC-WMS-023 | 盘点锁仓 | P1 | DotNet |
| TC-WMS-030～032 | PDA | P1 | DotNet, ApiHttp |
| TC-WMS-040 | Ifc 日志 | P2 | DotNet |
| TC-WMS-041 | Ifc 菜单 | P0 | Browser |
| TC-WMS-050～051 | Web 快捷 | P1 | Browser |
