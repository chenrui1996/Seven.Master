# WMS 与 WCS 实现说明

本文描述 Seven 中 **WMS 账本**、**立库（堆垛）WCS**、**四向车 WCS** 的现有实现逻辑。  
开关、表前缀与仿真入口见 [19-WMS与WCS包](./19-WMS与WCS包.md)；HotStore 见 [15](./15-热数据HotStore.md)。

产品形态：单进程 = WMS + 薄编排总线（`Bus_*`）+ 可插拔 WCS 包（`Stk_*` / `Fw_*`）。设备侧默认 `InMemoryEquipmentTriggerPort` 仿真。

---

# 第一部分：WMS

## 1. 总览

```text
ERP/MES / 运维 Vue3 / PDA App
        │
        ▼
   Seven.WebApi
        ├─ WMS（主数据·库存·入/出/盘点）
        ├─ ITransportOrderRequest → Bus_TransportOrder / Leg
        └─ IWcsPack.AcceptLeg → 各包任务（Stk_* / Fw_*）
```

单据收敛为三套：`Wms_InboundOrder`、`Wms_OutboundOrder`、盘点计划。  
数量账本只认 `Wms_Stock`；包内禁止第二套可扣减库存表。

---

## 2. 主数据与前缀

### 2.1 一仓多包

- `Wms_Warehouse.EnabledPackIds`：逗号分隔（如 `stacker,fourway`），至少一种。
- 库区 / 巷道 / 货位 / 层均带 **`PackId` 冗余列**。
- 跨包交接：`Wms_HandoverLink`（FromPack → ToPack + LocationCode）。
- 盘点锁仓：`Wms_Warehouse.IsCycleCountLocked`（立库分配会跳过该仓巷道）。

### 2.2 编码前缀

| PackId | 前缀 | 示例 |
|--------|------|------|
| `stacker` | `Stk.` | `Stk.A-01` / `Stk.B-01-02-03-1` |
| `fourway` | `Fw.` | `Fw.L02` / `Fw.A-L02-01` / `Fw.N-1205` |
| `boxsort` | `Bs.` | 预留 |

工具：`WcsPackIds`、`PackCodeRules`（校验/规范化前缀）。  
`LocationService` 拒绝 Pack 与前缀不一致的货位。

### 2.3 层级

```text
Wms_Warehouse
  └── Wms_Zone (PackId)
        └── Wms_Layer          ← 四向常用；堆垛可不建
              └── Wms_Aisle (PackId；四向可挂 LayerId)
                    └── Wms_Location (PackId；Row/Col/Layer/Depth 等)
```

### 2.4 Schema / Allocator 入口

| 接口 | 职责 |
|------|------|
| `IWcsLocationSchema` | 校验/规范化 Code |
| `IWcsLocationAllocator` | `AllocateInboundAsync` |
| `IWcsLocationAllocatorResolver` | 按 PackId 解析 |

| Pack | Schema | Allocator |
|------|--------|-----------|
| stacker | `StackerLocationSchema` | `StackerInboundAllocator`（巷→位） |
| fourway | `FourWayLocationSchema` | `FourWayInboundAllocator`（层→巷→位） |

策略参数在各包表（`Stk_AssignmentPolicy` / `Fw_LayerPolicy` 等），WMS 无总策略表。

---

## 3. 库存与容器

| 能力 | 实现 |
|------|------|
| 收货入账 | `StockService.ReceiveAsync`（+ 可选 `Wms_StockLedger`） |
| 发运扣账 | `StockService.ShipAsync` |
| 移库 | 上架确认 / `WmsTransportCompletionHandler` 内 Ship+Receive |
| 出库预留 | 审核时扣 `AvailableQty`；运输完成再正式 Ship |

容器：`Wms_Container.LocationCode` 与货位 `CurrentContainerCode` / `IsOccupied` / `IsBooked` 联动。

运输完成处理器要点：

- 移库存（From→To）
- 占用目标货位；出库 / `StackerTransfer` 释放源位
- 按 `RefType` 回写入库 Detail / 出库单完成态

---

## 4. 入库

### 4.1 单据结构

```text
Wms_InboundOrder (Draft → Approved → Executing → Completed)
  ├── Lines（计划量 / CompletedQty）
  └── Details（组盘执行明细）
        Status: Created → Transporting → Completed
```

实现：`InboundOrderService`。

| 方法 | 行为 |
|------|------|
| `CreateAsync` / `ApproveAsync` | 建单、审核 |
| `BuildPalletAsync` | 写 Detail → 收货位入账 → 可选 Allocator 目标位 → 异址则挂 Bus |
| `ReceiveAndBuildPalletAsync` | 整单组盘兼容入口 |

**组盘分支：**

1. 解析收货位、PackId（请求 / 前缀 / 仓 EnabledPackIds）。
2. 无目标且允许分配 → 对应包 `AllocateInboundAsync`（常 Booking）。
3. 收货位入账；Detail 记 Receive/Target/Aisle/PackId。
4. 收货位≠目标位且有容器 → `ITransportOrderRequest`（Ref 挂 Detail）。
5. **同址平库**：不建运输，Detail 可直接 Completed。

### 4.2 与总线

```text
BuildPallet → Bus_TransportOrder + Leg(PackId)
  → 包 AcceptLeg → 包内任务
  → 设备/仿真 → SegmentFeedback → Bus Complete
  → WmsTransportCompletionHandler → 库存到目标位、Detail/单完成
```

API：`api/WmsInboundOrder`（`getPageData` / `add` / `approve` / `buildPallet` / `receive`）。

---

## 5. 出库

实现：`OutboundOrderService`。

- 头：`WcsGroupNo`（默认同单号）
- 行：`WcsPri`（默认行号）、From/To、ContainerCode

| 方法 | 行为 |
|------|------|
| `ApproveAsync` | 需运输：预留 AvailableQty → 建运 `RefType=OutboundOrder` |
| `ShipAsync` | 平库直发；已挂运则禁止手发，等完成回调 |
| `AllocateAndReserveAsync` | 仅预留不建运 |

**NeedsTransport**：有 From 且 To 不同。  
包侧：`AcceptRetrieval`（堆垛 / 四向）按组 Pri 滚动执行。

API：`api/WmsOutboundOrder`（`add` / `approve` / `ship`）。

---

## 6. 盘点

实现：`CycleCountService`。

| 方法 | 行为 |
|------|------|
| `CreatePlanAsync` | 建行；`BookQty` 取当前库存（无则 0） |
| `RecordCountAsync` | 写实盘与 Diff；Draft→Executing |
| `ConfirmAdjustAsync` | 全部已盘后 Diff>0 Receive / Diff<0 Ship；单 Completed |

API：`api/WmsCycleCount`。Vue：`CycleCount.vue`。

---

## 7. PDA（Seven.App）

薄封装 WMS：`PdaService` / `PdaController`（JWT，`/api/pda/*`）。

| 能力 | 路径摘要 |
|------|----------|
| 菜单 | `GET /api/pda/menu`（收货/上架/盘点） |
| 平库收货 | pending 入库单 → `BuildPallet`（常同址） |
| 平库上架 | pending Detail → 扫目标位移库确认 |
| 盘点 | pending 计划 → 按库位录实盘 → confirm 调账 |

工程：`Seven.App/`（uni-app）。未做：拣选、发货、离线队列、立库异常页。

---

## 8. 运维前端与总线

Vue3：`Location` / `Stock` / `InboundOrder` / `OutboundOrder` / `CycleCount`；总线 `Wcs/Bus/TransportOrder`。  
Features 仅隐藏菜单，不卸载后端服务。

总线：

- 表：`Bus_TransportOrder`、`Bus_TransportLeg`
- WMS 只提交 From/To/容器/业务引用；Leg 带 PackId
- 包 `CanHandle` / `AcceptLeg` / `CancelLeg` / `QueryLeg`

---

## 9. WMS 端到端速查

| 场景 | 步骤 |
|------|------|
| 立库入库 | 建单审核 → buildPallet → SUDR → 段反馈 → 库存到目标位 |
| 平库 PDA | 收货同址完成 → 上架移库确认 |
| 立库出库 | 建单（组号/Pri）→ 审核建运 → Retrieval 滚动 → 完成扣账 |
| 盘点 | 建计划 → 录实盘 → 确认调账 |

---

# 第二部分：立库 WCS（堆垛机）

PackId=`stacker`；表前缀 `Stk_`；货位前缀 `Stk.`。  
入口：`StackerWcsPack`；目的地：`StackerDestinationService`；宿主：`StackerSchedulerHostedService`。

## 1. 任务与表

| 对象 | 用途 |
|------|------|
| `Stk_PutAwayTask` | 入库上架（Leg 1:1） |
| `Stk_RetrievalTask` | 出库取货；`WcsGroupNo` / `WcsPri` |
| `Stk_DeviceTask` | 设备段；`Seq` / `ExeStackCode` / From·Dest 点 |
| `Stk_RequestPoint` | 申请点（Aisle / Location / Blocking…） |
| `Stk_AssignmentPolicy` / `Record` | 巷道策略与轮转 |
| `Stk_Route` / `RouteFlow` / `DeviceCoder` | 路网、占边、点码映射 |
| `Stk_LocationProfile` | 双深组 / LockBin |

入库 Ref → PutAway；出库 `OutboundOrder` / `StackerTransfer` → Retrieval。

---

## 2. 申请与分配（入库 SUDR）

```text
Bus → PutAway(Accepted)
  → DestinationRequest @ AisleRequest
       · CheckResult≠OK → Reject + PutAway Failed
       · SelectAisle → AisleAssigned → Dispatch(Ep) + DeviceTask
  → （可选）LocationRequest → SelectLocation+Book → Dispatch(Bin)
  → （可选）BlockingPoint：作废在途段、释放预约、重选
  → SegmentFeedback → 段推进 / PutAway Completed → Bus → WMS
```

双包共享 TriggerPort：若 SourcePoint 为启用的 **四向** 申请点且外形 NG，堆垛侧 **静默**（不误 Reject）。

### 2.1 SelectAisle

过滤：策略可用、高重；`Wms_Aisle.IsAvailable`；盘点锁仓跳过；`MinEmptySlots`（有货位主数据时）。  
排序：`AllocationWeight` ↓ → `LastAssignedAt` ↑ → AisleCode。  
结果：策略 `DestinationPointCode` 或巷道 `EpPointCode`；写 `AssignmentRecord`。

### 2.2 SelectLocation

- 同巷、`PackId=stacker`、空闲
- 排除 `InLockBin`；深位仅当同组更浅已占用/预约（防孤二深）
- 排序：Depth → Layer → Column → Row → Code
- Booking：目标 `IsBooked`；同 `BinGroupCode` 其它空位一并预约

---

## 3. 寻路与拆腿

```text
得到目的地点 dest
  → StackerPathDispatcher
       · DeviceCoder 判图
       · Dijkstra（满容量边不可用）
       · 连续同 ExeStackCode 合并为一段
       · 多条 DeviceTask + RouteFlow；仅首段 Dispatched
SegmentFeedback
  → 释流 → 下一段 Dispatched → …
  → 无下一段：PutAway/Retrieval Completed → Bus
```

无路网数据：**单段** DeviceTask（与仅分配阶段行为兼容）。  
出库 Retrieval 下发同样走 PathDispatcher（From→To）。

---

## 4. 出库 Retrieval 与深浅移库

```text
Approve → AcceptRetrieval(Accepted)
  → TryDispatch：同组更小 Pri 未终态则 Suspended
  → StackerDepthGuard：深位被浅位挡
       · 深位 Suspended，WcsPri+1
       · 建 Bus StackerTransfer（浅→同巷空位，原 Pri）
  → PathDispatcher 下发
段完成 → 同组下一 Pri 滚动
```

`StackerTransfer` 完成：移库存、释放源位、占用目标；完成后可再派深位。

---

## 5. 立库关键类型

| 类型 | 路径（均在 `Infrastructure/Wcs/Packs/Stacker/`） |
|------|--------------------------------------------------|
| `StackerAisleAllocator` / `LocationAllocator` | 分配 |
| `StackerDoubleDeepRules` / `DepthGuard` | 双深 / 深浅移库 |
| `StackerRouter` / `PathDispatcher` | 寻路拆腿 |
| `StackerDestinationService` | SUDR / 段反馈 |
| `StackerInboundAllocator` | WMS 组盘入口 |

---

## 6. 立库测试过滤

```powershell
dotnet test --filter "FullyQualifiedName~Stacker|FullyQualifiedName~InboundToStacker|FullyQualifiedName~OutboundToStacker|FullyQualifiedName~StackerDoubleDeep|FullyQualifiedName~StackerPath|FullyQualifiedName~StackerRouter"
```

---

# 第三部分：四向车 WCS

PackId=`fourway`；表前缀 `Fw_`；货位前缀 `Fw.`。  
入口：`FourWayWcsPack`；目的地：`FourWayDestinationService`；宿主：`FourWaySchedulerHostedService`（SUDR + 周期：Path 重试 / 停车唤醒 / Hoist 补扫）。  
热路径键命名空间：`fw:`（生产禁用 Demo 全局 `wcs:`）。

## 1. 任务与表

| 对象 | 用途 |
|------|------|
| `Fw_LayerPolicy` / `Fw_AislePolicy` / `Fw_AssignmentRecord` | 层/巷策略与轮转 |
| `Fw_PutAwayTask` | 入库；AssignedLayer/Aisle/Location |
| `Fw_RetrievalTask` | 出库；WcsGroupNo/WcsPri |
| `Fw_ShuttleTask` / `Fw_ShuttleTaskPath` | 设备载体与路径点列 |
| `Fw_RequestPoint` | Layer/Aisle/Location；另有 Hoist/Shuttle 口类型（SUDR 不误拒） |
| `Fw_MapVersion`（含 LayerCode）/ `Node` / `Route` | 层内路网 |
| `Fw_ParkingLedger` | Free / Reserved / Occupied（含 Layer/Aisle 作用域） |
| `Fw_HoistDevice` / `LayerPoint` / `Task` / `ExecTask` | 提升机 |

`CanHandle`：两端 `Fw.` 或已知交接；纯 `Stk.*` 拒接。

---

## 2. 分配与入库

```text
BuildPallet（FourWayInboundAllocator → Fw.* + Booking）
  → AcceptLeg：PutAway + Shuttle(Accepted)
  → SUDR Layer/AisleRequest → 选层巷 → Dispatch(Ep)
  → （可选）LocationRequest → Book → Dispatch(Bin)
  → SegmentFeedback（可多段，见下）→ Completed → Bus → WMS
```

双包：外形 NG 时若点为启用 **堆垛** 申请点，四向侧静默。

### 2.1 SelectLayer / Aisle / Location

- **层**：`Fw_LayerPolicy` 可用、高重；对齐 `Wms_Layer`；权重 + 轮转。
- **巷**：同层策略；`MinEmptySlots` **按本层**空闲计数；`MaxShuttleCount`：该层巷 `Reserved+Occupied` 达上限则跳过；Ep=`DestinationPointCode`。
- **位**：同仓同巷空闲；Booking `IsBooked`。

---

## 3. 层内寻路与交通

```text
PathDispatcher
  → ResolveMapVersionId：显式 Id → LayerCode → Active → 首条
  → Dijkstra（FourWayRouter）→ 写 ShuttleTaskPath
  → TrafficGuard.TryGrant 首跳 → DispatchDestination
SegmentFeedback → Advance：释边 → 占下一段 → …
末段 → PutAway/Retrieval Completed → Bus
```

无 Map/无边：**单段** From→To。  
周期：`Routing` 占边失败可重试；Cancel/Fail 释尽该车边（path EdgeId + owner 索引；HotStore `fw:flow:`）。

---

## 4. 出库与停车

```text
OutboundApprove → AcceptRetrieval
  → TryDispatch：Pri 门闩
       · 原子预订停车（WHERE Status=Free）
       · PathDispatcher；仅 shuttle Running 后 Occupied + Retrieval Dispatched
       · 占边失败保持 Routing/Reserved；成功后可 Promote
       · 无空闲车位 → Suspended（周期有 Free 再唤醒）
段完成 → 释停车 → 同组下一 Pri
```

`RefType=OutboundOrder|FourWayTransfer` → Retrieval（同层）。跨层见下一节。

---

## 5. 提升机（跨层）

**策略**：单 Bus Leg + 包内三阶段（不拆多段总线）。

```text
AcceptLeg 发现 From/To 不同层
  → 选口（Available 机 + 空闲口；OutboundOrder 优先 Outbound Ap/Ep）
  → HoistTask + Exec(Queued) + Shuttle
  → ToSrcAp：货位 → 源层 Hoist AP
  → HoistLift：同口排队（Pri/时间）；Dispatch 目标层 EP
  → FromDesEp：目标层 EP → 目标货位
  → Completed → Bus → WMS
```

联锁：同口已有 Dispatched → 后到 Suspended；完成/失败/周期补扫唤醒队列。  
`FeedbackCode≠OK`：不推进阶段（抬升段可 Failed；到 AP/目标可保持重试）。  
段反馈优先由 `FourWayHoistOrchestrator` 消费。

---

## 6. 四向关键类型

| 类型 | 路径（`Infrastructure/Wcs/Packs/FourWay/`） |
|------|----------------------------------------------|
| `FourWayInboundAllocator` | 三阶段分配 |
| `FourWayDestinationService` | SUDR / 段反馈 |
| `FourWayPathDispatcher` / `Router` / `TrafficGuard` | 寻路占边 |
| `FourWayHoistOrchestrator` | 跨层 |
| `FourWayWcsPack` | 接单 / Retrieval / 停车 / CanHandle |

---

## 7. 四向测试过滤

```powershell
dotnet test --filter "FullyQualifiedName~FourWay|FullyQualifiedName~InboundToFourWay|FullyQualifiedName~OutboundToFourWay|FullyQualifiedName~StackerPack"
```

---

# 附录

## A. 表前缀速查

| 前缀 | 模块 |
|------|------|
| `Wms_` | 主数据与库存单据 |
| `Bus_` | 编排总线 |
| `Stk_` | 堆垛 WCS |
| `Fw_` | 四向 WCS |

## B. 未纳入本文运行时正文、另见专项文档的能力

- **仿真联调闭环（编辑地图→Deploy→Trigger/Gateway→Promote）**：[21-仿真器与联调闭环](./21-仿真器与联调闭环.md)、[`Seven.Simulator`](../Seven.Simulator/)
- Phase H：真机 DeviceComm 替换 TriggerPort（见 [16](./16-设备通讯DeviceComm.md) 与 Promote）
- 堆垛 SupperRoute 多种子与时间窗；OutLockBin 硬拒出  
- PDA 拣选/发货/离线；ERP 全量接口产品化  

## C. 相关文档

| 文档 | 用途 |
|------|------|
| [19-WMS与WCS包](./19-WMS与WCS包.md) | Features、前缀、仿真、上线清单 |
| [21-仿真器与联调闭环](./21-仿真器与联调闭环.md) | Simulator 实施与开发四期 |
| [14-功能开关](./14-功能开关.md) | Features 总控 |
| [15-热数据HotStore](./15-热数据HotStore.md) | 四向 `fw:` 热路径 |
| [`Seven.App/README.md`](../Seven.App/README.md) | PDA 运行 |
| [`design/wms/`](../design/wms/)、[`design/srm-wcs/`](../design/srm-wcs/)、[`design/shuttle-wcs/`](../design/shuttle-wcs/) | 设计原稿 |
