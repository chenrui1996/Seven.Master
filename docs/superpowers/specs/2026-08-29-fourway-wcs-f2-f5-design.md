# 四向车 WCS 全量对齐（F2–F5）

状态：冲刺①（F2+F3）与冲刺②（F4+F5）均已落地（2026-08-29）  
日期：2026-08-29  
依据：`design/shuttle-wcs/`；对话锁定：范围 F2–F5、交付节奏 B、Hoist 完整模块  
不含：F6 Simulator Promote、真机 DeviceComm（Phase H）

---

## 1. 目标

在 F0/F1（`Wms_Layer`、`Fw.` 前缀、同仓共存）之上，对齐 RCS4Shuttle 运行核：

- **F2**：层→巷→位分配策略（独立 `Fw_*Policy`，不与 `Stk_` 混表）
- **F3**：入库 PutAway + 申请点 + 仿真 Trigger + 入库 E2E
- **F4**：层内寻路/占边/分段推进闭环
- **F5**：出库 Retrieval、停车账本、**完整 Hoist**（EP/AP、排队、跨层 Exec、与 Shuttle 联锁）

## 2. 交付节奏（方案 B）

| 冲刺 | 内容 | 产品文档 |
|------|------|----------|
| **①** | F2 + F3 | `doc/24-WCS四向分配与入库.md` |
| **②** | F4 + F5 | `doc/25-WCS四向寻路与出库.md`、`doc/26-WCS四向提升机.md` |

两冲刺均回写 `design/shuttle-wcs/02-migration-plan.md`、`doc/19`/`20`/`README`。

## 3. 架构

```text
WMS 组盘/出库 → Bus Leg
  → FourWayWcsPack.AcceptLeg（CanHandle 仅 Fw. / 交接）
       · 同层：Fw_PutAway | Fw_Retrieval → Fw_ShuttleTask(+Path)
       · 跨层：源层 Shuttle → Fw_Hoist* → 目标层 Shuttle
  → DestinationRequest / SegmentFeedback（IEquipmentTriggerPort 仿真）
  → Bus Complete → WMS 落账
```

约束（与现裁定一致）：

- 统一 `Wms_*` 账本；PackId=`fourway`；码前缀 `Fw.`
- 路径/流量/分配策略表前缀 `Fw_`，禁止复用 `Stk_AssignmentPolicy`
- HotStore 键命名空间 `fw:`；生产 `EnableDemoScheduler=false`
- 不引 RCS4Shuttle 程序集；不整包复制 `ShuttleExecuteStack`

---

## 4. 冲刺①：F2 + F3

### 4.1 F2 分配

| 对象 | 说明 |
|------|------|
| `Fw_LayerPolicy` | 已有；补 `AllocationWeight`（及必要时空位/轮转相关字段） |
| `Fw_AislePolicy` | **新建**：LayerCode+AisleCode、MinEmptySlots、MaxShuttle、EpPoint、权重、可用 |
| `Fw_AssignmentRecord` | **新建**：层/巷最近分配时间与次数 |
| `FourWayInboundAllocator` | SelectLayer → SelectAisle → SelectLocation + **Booking** |

规则（可测简化，对齐 RCS 核心）：高重过滤 → 层权重+轮转 → 巷空位/可用/MaxShuttle → 位排序 → `IsBooked`。  
WMS `BuildPallet` 已走 `IWcsLocationAllocator`，F2 改完即生效。

### 4.2 F3 入库运行

| 对象 | 说明 |
|------|------|
| `Fw_PutAwayTask` | Leg 1:1；AssignedLayer/Aisle/Location；状态机对齐堆垛 PutAway |
| `Fw_RequestPoint` | Layer / Aisle / Location 申请（F5 再扩 Hoist*） |
| `FourWayDestinationService` | DestinationRequested / SegmentFeedback；拒收语义对齐堆垛 |
| `FourWayWcsPack` | AcceptLeg 写 PutAway；**CanHandle** 收窄为四向前缀（或一端交接） |

时序：

```text
BuildPallet(Allocator→Fw.*) → Bus → PutAway(Accepted)
  → SUDR Layer/Aisle → Dispatch(Ep)
  → （可选）LocationRequest → Book → Dispatch(Bin)
  → SegmentFeedback → Completed → Bus → WMS
```

F3 **不做**层内多段寻路（F4）与跨层（F5）。无地图时允许单段反馈即完成。

### 4.3 冲刺①验收

1. Allocator 金样单测（层/巷/位可断言）  
2. 入库 E2E：组盘→申请→完成→库存在 `Fw.*`  
3. 纯 `Stk.*` 腿不被 FourWay 接单  

---

## 5. 冲刺②：F4 + F5

### 5.1 F4 层内闭环

| 对象 | 说明 |
|------|------|
| 已有 | `Fw_Map/Node/Route`、`FourWayRouter`、`FourWayTrafficGuard` |
| `FourWayPathDispatcher` | 按层 Map 最短路 → `Fw_ShuttleTaskPath` → 占边 → 分段推进 |
| 回退 | 无边/无 Map：单段完成（同堆垛 S2） |

### 5.2 F5a 出库与停车

| 对象 | 说明 |
|------|------|
| `Fw_RetrievalTask` | WcsGroupNo / WcsPri；同组滚动 |
| `Fw_ParkingLedger` | 预订 / 占用 / 释放 |
| 派车 | 空闲车 + 停车账本；无车挂起 |

深浅：可读 `Wms_Location.Depth`；**不**复用 `Stk_LocationProfile`（`Fw_LocationProfile` 本轮非必须）。

### 5.3 F5b 完整 Hoist

| 表 | 对齐 RCS |
|----|----------|
| `Fw_HoistDevice` | 提升机台账 |
| `Fw_HoistLayerPoint` | 层 ↔ 入/出 EP·AP |
| `Fw_HoistTask` | Src/Des Layer+Address、容器、关联 Bus/Leg |
| `Fw_HoistExecTask` | 排队、下发、完成/失败 |
| `Fw_RequestPoint` 扩类型 | HoistInbound/Outbound EP·AP、Shuttle EP·AP |

跨层：

```text
源层 Shuttle（货位→本层 Hoist AP）
  → HoistExec（源层→目标层，仿真 Trigger）
  → 目标层 Shuttle（Hoist EP→目标货位）
```

联锁：同提升口冲突时后到挂起；排队按 Pri/CreateTime。  
下发/反馈仍走 `IEquipmentTriggerPort`，不接真 PLC。

### 5.4 冲刺②验收

1. 有 Map：多节点占边/释流；无 Map：单段仍通  
2. 出库：Retrieval + Pri 滚动 + 停车预订/释放  
3. 跨层 E2E：源层→Hoist→目标层，库存到目标 `Fw.*`  
4. 同口冲突：后到挂起  

---

## 6. 明确不做

- F6 Promote / 真 DeviceComm  
- 与 SRM 共用 AssignmentPolicy 运行表  
- 整包复制 `ShuttleExecuteStack` / `HoistExecuteStack` 巨型类  
- 引用 RCS4Shuttle 程序集  

---

## 7. 实现顺序建议

1. 冲刺①：迁移与 Policy 表 → Allocator + 单测 → PutAway/Destination/CanHandle → 入库 E2E → doc/24  
2. 冲刺②：PathDispatcher → Retrieval/Parking → Hoist 表与编排 → 跨层 E2E → doc/25–26  

用户批准本 spec 后，再写 `docs/superpowers/plans/` 实施计划并开工。
