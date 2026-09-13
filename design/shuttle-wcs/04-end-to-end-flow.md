# 四向穿梭车：端到端完整流程（WMS ↔ Bus ↔ Fw）

状态：设计定稿  
日期：2026-09-12  
实现对照：[`doc/20-WMS与WCS实现说明.md`](../../doc/20-WMS与WCS实现说明.md) 第三部分  
源调研：[`01-rcs4shuttle-flows.md`](./01-rcs4shuttle-flows.md)、RCS4Shuttle `ShttleInfAlign`  
运维 IA：[`../ops/01-ops-menu-restructure.md`](../ops/01-ops-menu-restructure.md)

本文补齐「穿梭车完整流程」：从主数据到入/出/移库/充电/指定点/跨层提升与仿真触发的**逻辑闭环**。  
不替代包内类级实现说明；实现以 `Infrastructure/Wcs/Packs/FourWay/` 为准。

---

## 0. 分层与硬规则（主流 WMS/WCS 对齐）

```text
ERP/MES / Vue 运维 / PDA / Simulator
        │
        ▼
   WMS 账本（Wms_*：单据·库存·容器·库位权威）
        │ ITransportOrderRequest
        ▼
   编排总线（Bus_TransportOrder / Leg；只编排，不寻路）
        │ AcceptLeg / AcceptRetrieval
        ▼
   FourWay 包（Fw_*：分配·寻路·交通·穿梭·提升）
        │ IEquipmentTriggerPort
        ▼
   仿真 或 DeviceComm（段反馈 → 完成 Leg → WMS 回写）
```

| # | 硬规则 |
|---|--------|
| 1 | 可扣减库存只在 `Wms_Stock`；Fw 表禁止第二套账本 |
| 2 | 路径边/占道只在 `Fw_Route*` + HotStore `fw:` |
| 3 | 跨层用**单 Bus Leg + 包内 Hoist 三阶段**，不拆多段总线 |
| 4 | 运维调度走主干；手动控制旁路默认折叠且受双空闲门控 |
| 5 | 一仓多包：货位前缀 `Fw.`；交接靠 `Wms_HandoverLink` |

---

## 1. 主数据与编码

```text
Wms_Warehouse (EnabledPackIds 含 fourway)
  └── Wms_Zone (PackId=fourway)
        └── Wms_Layer                 ← 四向必用
              └── Wms_Aisle (LayerId；EpPoint)
                    └── Wms_Location (Fw.*；Row/Col/Layer/Depth；Gateway/Bin…)
```

包内配套：

| 表 | 作用 |
|----|------|
| `Fw_LayerPolicy` / `Fw_AislePolicy` / `Fw_AssignmentRecord` | 层/巷分配与轮转 |
| `Fw_RequestPoint` | Layer/Aisle/Location/Hoist/Shuttle 口 |
| `Fw_MapVersion` + `Fw_Node` + `Fw_Route` | 层内路网 |
| `Fw_ParkingLedger` | 车位 Free/Reserved/Occupied |
| `Fw_HoistDevice` / `Fw_HoistLayerPoint` | 提升机与层口 |

编码：`PackCodeRules` 强制 `Fw.` 前缀；`FourWayLocationSchema` 校验。

---

## 2. 对象层级（调度意图 → 设备段）

对齐 RCS「ShuttleTask → Transport → Exec → Step」，Seven 收敛为：

```text
Bus_TransportOrder
  └── Bus_TransportLeg (PackId=fourway)
        ├── Fw_PutAwayTask | Fw_RetrievalTask     ← 业务锚点
        ├── Fw_ShuttleTask (+ Fw_ShuttleTaskPath) ← 层内执行载体
        └── Fw_HoistTask → Fw_HoistExecTask×N     ← 仅跨层
```

| 状态（示意） | PutAway/Retrieval | ShuttleTask | HoistExec |
|--------------|-------------------|-------------|-----------|
| 接单 | Accepted | Accepted / Routing | Queued |
| 执行 | Dispatched / Executing | Running | Dispatched |
| 等待 | Suspended（Pri/停车/同口） | Reserved | Suspended |
| 终态 | Completed / Failed / Cancelled | Completed / … | Completed / Failed |

设备侧另有**段空闲**：当前段完成前不得下发下一段（双空闲之一）。

---

## 3. 入库全流程（正式账本）

### 3.1 主链

```text
WMS 建入库单 → Approve
  → BuildPalletAsync
       · 收货位入账（Stock Receive）
       · FourWayInboundAllocator：层→巷→位（常 Booking）
       · From≠To → ITransportOrderRequest（Ref=InboundDetail）
  → Bus Leg → FourWayWcsPack.AcceptLeg
       · 建 Fw_PutAwayTask + Fw_ShuttleTask(Accepted)
  → SUDR / DestinationRequest（仿真或设备）
       · Layer|AisleRequest → SelectLayer/Aisle → Dispatch(Ep)
       · LocationRequest → SelectLocation+Book → Dispatch(Bin)
  → PathDispatcher：Dijkstra → ShuttleTaskPath → TrafficGuard 占首跳
  → SegmentFeedback 循环推进段
  → PutAway Completed → Bus Complete
  → WmsTransportCompletionHandler：容器落到目标位、Detail/单完成
```

### 3.2 分配三阶段

| 阶段 | 入口 | 结果写回 |
|------|------|----------|
| 层 | `Fw_LayerPolicy` + `Wms_Layer` | PutAway.AssignedLayer |
| 巷 | 同层策略；空位/车数上限 | AssignedAisle；Ep 点 |
| 位 | 同巷空闲 Bin；可延迟到 LocationRequest | AssignedLocation；IsBooked |

WMS 组盘时 Allocator 可提前选位；设备侧 LocationRequest 可复核/补选（与 RCS「货位常延迟到寻路」兼容）。

### 3.3 运维轻量入库（可选旁路）

见 [`05-ops-menu-and-process.md`](./05-ops-menu-and-process.md)：运维「入库」页可走 **轻量 Container + 包内 CreateAndDispatch**（对标 RCS `ShuttleInboundService`），**默认不建 Stock**。  
若现场需要账本：勾选「同步 WMS」则改走 `BuildPallet` 正式链。  
**全库出入互斥**：存在未完成入库则拒出库创建，反之亦然（包级门控）。

---

## 4. 出库全流程

```text
WMS 建出库单 → Approve
  · GenerateFromOutbound → PickingTask
  · Stock.Book 扣 AvailableQty
  · NeedsTransport → Bus（RefType=OutboundOrder）
  → AcceptRetrieval
       · 同组更小 WcsPri 未终态 → Suspended
       · 原子预订停车位（ParkingLedger Free→Reserved）
       · PathDispatcher；车 Running 后 Occupied + Retrieval Dispatched
  → SegmentFeedback → 释停车 → 滚动下一 Pri
  → Completed → Bus → Ship/移库回写 → 出库完成
```

深浅/阻挡：同层移库可用 `RefType=FourWayTransfer`（对标堆垛 `StackerTransfer`）；跨层走 Hoist。

---

## 5. 同层移库 / 指定点 / 充电 / 避让

| 场景 | 触发 | 主干行为 |
|------|------|----------|
| 同层移库 | WMS 或运维 | Bus Leg From≠To 同层；PutAway/专用 Transfer 语义 |
| 指定点 | 监控/穿梭运维 | 建 DirectDispatch 类 `Fw_ShuttleTask`（WCS Free 门控）；走 PathDispatcher |
| 充电 | 运维常用功能 | Charge 调度；结束充电释放 |
| 避让 | 交通决策 | Path 重规划 / Avoid 类任务；HotStore 释边重占 |

RCS 枚举对照（实现可用内部 DispatchType，不必 1:1 暴露）：Inbound=31、Outbound=40、Transfer=20、Charge=60、Direct=80、Avoid*=81–83。

---

## 6. 跨层（提升机）三阶段

```text
AcceptLeg 发现 From/To 不同 LayerCode
  → 选 HoistDevice + 源/宿层口（Outbound 优先出库口）
  → Fw_HoistTask + Exec(Queued) + Shuttle
  → 阶段1 ToSrcAp：货位/点 → 源层 Hoist AP（Shuttle 段）
  → 阶段2 HoistLift：同口排队；Dispatch 目标层 EP
  → 阶段3 FromDesEp：目标层 EP → 目标货位
  → 全完成 → Bus → WMS
```

联锁：同口已有 Dispatched → 后到 Suspended；完成/失败/周期补扫唤醒。  
`FeedbackCode≠OK`：抬升段可 Failed；到 AP/目标可保持重试（不盲目推进）。

---

## 7. 与仿真 Trigger 的对应

| 仿真 API | 流程位置 |
|----------|----------|
| `POST /api/Wcs/Triggers/destination-request` | SUDR：外形/源址 → 分配 → 首段 Dispatch |
| `POST /api/Wcs/Triggers/segment-feedback` | 段完成：释边 → 下一段或 PutAway/Retrieval/Hoist 推进 |

双包共享 TriggerPort：源点属堆垛且外形 NG 时，四向侧**静默**（不误 Reject）。

Simulator Promote：地图/策略/设备主数据进生产库；运行时任务不 Promote（见仿真设计）。

---

## 8. 异常与恢复矩阵（流程级）

| 现象 | 系统行为 | 运维动作 |
|------|----------|----------|
| 分配无空位 | Accept/SUDR 失败或 PutAway Failed | 改策略/清预约/指定货位 |
| 占边失败 | Shuttle Routing 重试 | 监控看冲突；必要时取消释边 |
| 无停车位 | Retrieval Suspended | 等 Free 或人工结束占用账本 |
| 提升同口排队 | Exec Suspended | 等前车完成；勿强推抬升 |
| 段反馈 NG | 不推进或 Failed | 强制完成（确认现场已到位）/ 重发 |
| 急停联锁 | 拒新派发 | 联锁页恢复后重试 |

详细按钮与 API 见 [`05`](./05-ops-menu-and-process.md)。

---

## 9. 状态机速查（入库）

```text
Wms_InboundOrder: Draft → Approved → Executing → Completed
Detail: Created → Transporting → Completed
Bus_Leg: Created → Accepted → Executing → Completed|Failed
Fw_PutAway: Accepted → (Aisle/Location Assigned) → Dispatched → Completed
Fw_Shuttle: Accepted → Routing → Running → Completed
```

出库对称：Order Approved（已 Book）→ Leg → Retrieval → Shuttle → Complete → Stock Ship。

---

## 10. 与旧文档关系

| 文档 | 关系 |
|------|------|
| `01-rcs4shuttle-flows.md` | RCS 源调研；本文为 Seven 目标闭环 |
| `02-migration-plan.md` | 迁移阶段；F6 等仍以迁移计划为准 |
| `doc/20` | 实现说明；本文为流程规格，冲突时以代码+本文修订为准 |
| 缺失的 `doc/24–27` | 后续可将本文拆章同步到 `doc/` |

---

## 11. 验收口径（流程闭环）

1. 仅开启 `fourway`：组盘 → 仿真 SUDR+反馈 → 库存到 `Fw.*` 目标位。  
2. 同层出库：审核 Book → 停车预订 → 段完成 → 扣账。  
3. 跨层：单 Leg 内三阶段顺序完成，总线不出现三段 Leg。  
4. 运维指定点/充电在 WCS Free 时成功，非 Free 被拒。  
5. 出入库互斥门控在运维入库/出库创建上生效。
