# RCS4Shuttle：四向车 WCS 流程说明书

状态：调研完成  
日期：2026-08-29  
源码：`D:\Junheinrich\Shuttle\RCS4Shuttle`  

---

## 1. 工程定位

| 区域 | 路径 | 职责 |
|------|------|------|
| 主数据 | `LES.Entities\Models\BasicData\` | Zone / **Layer** / Aisle / Location / AssignmentPolicy |
| WCS 设备与任务 | `LES.Entities\Models\WCS\` + `WCS\Shuttle\` + `WCS\Hoist\` | TransportTask、LesRoute、Shuttle*、Hoist* |
| 入库锚点 | `Models\WMS\Inbound\StorageTask4Shuttle.cs` | 继承 StorageTask，多 LayerCode/AisleCode |
| 分配引擎 | `LES.BLL\...\Location\LocationExecuteStack.AllocationEngine4Shuttle.cs` | SelectLayer → Aisle → Location |
| 堆垛对照引擎 | 同目录 `AllocationEngine4SRM.cs` | SelectAisle → Location（**同栈、同主数据表**） |
| 穿梭执行 | `...\DeviceStack\Shuttle\ShuttleExecuteStack.cs` | DestinationRequest、调度、协议 |
| 提升机 | `...\DeviceStack\Hoist\` | 跨层；HoistLayerPoint |
| 路径 | `...\RouteStack\` + `LesRoute` | 与 SRM 同源概念，MAP 按层 |

**关键事实：** RCS 在**同一套 BasicData 表**上跑 SRM 与 Shuttle 两套分配引擎；用 `Zone.DeviceType` 区分设备族，Shuttle 多一张 **Layer** 主表。

---

## 2. 货位 / 层级主数据

```text
Warehouse
  └── Zone（库区；DeviceType = Shuttle / SRM / …）
        └── Layer（楼层主数据，四向专用）
              └── Aisle（巷道/轨道；挂 LayerCode）
                    └── Location（货位/节点；LayerCode + AisleCode + X/Y/Z + Row/Col/Layer/Deepth）
```

| 实体 | 文件 | 四向要点 |
|------|------|----------|
| Zone | `BasicData\Zone.cs` | `DeviceType` 决定走 Shuttle 还是 SRM 分支 |
| **Layer** | `BasicData\Layer.cs` | Code、ZoneCode、Attributes、AllcationWeight、MaxRow/Col/Deepth、Z、MaxShuttleCount、DevicePoint |
| Aisle | `BasicData\Aisle.cs` | **LayerId/LayerCode**；EpPoint；Cur/MaxShuttleCount；ReservedBinCount |
| Location | `BasicData\Location.cs` | LayerCode、AisleCode、Map、ShuttleParkFlag、IsBindingLift；坐标 X/Y/Z 与 Row/Col/Layer/Deepth |
| AssignmentPolicy | `BasicData\AssignmentPolicy.cs` | 同时可有 **LayerCode + AisleCode**；高重、Next 轮转链 |

申请点扩展类型（加载于 LocationExecuteStack）：HoistInbound/Outbound EP/AP、Shuttle EP/AP、RequestPassPlcPoint（同址可既接输送又接提升机）。

---

## 3. 分配流水线（四向）

实现：`LocationExecuteStack.AllocationEngine4Shuttle.cs`

```text
SelectLayer / SelectLayerAndAisle
  · key = warehouse + zone + FunctionalArea
  · AssignmentPolicy：高重 + Layer 可入 + ClassifyFlag 轮转
  · 同层选巷道（空位、混码、小车数、与申请点距离）

→ StorageTask4Shuttle：AisleAssigned（写入 LayerCode/AisleCode）

→ 货位常延迟到寻路：RouteCalculation.HandleInboundOrTransferRoute
  · SelectLocation(aisle) → Booking；深位优先等
  · 回填 LocationAssigned + PutDownAddress

（备选）车到 LocationRequest 点再选货位并建 InboundPutDown
```

状态：`Create → (LayerAssigned) → AisleAssigned → LocationAssigned → Finish`。

**实现注意（RCS 现状）：** `SelectLayer` / `SelectLayerAndAisle` 实现完整，但对外调用链偏弱；现场更多依赖任务已带巷道码 + **寻路时 SelectLocation**。迁 Seven 时应把三阶段收成稳定的 `IWcsLocationAllocator`，避免「策略写了却只在寻路里隐性选位」。

与堆垛（同仓库 `AllocationEngine4SRM.cs`）对比：

| | Shuttle | SRM |
|--|---------|-----|
| 阶段 | **层 → 巷道 → 货位**（货位可延迟） | **巷道 → 货位** |
| 策略锚点 | Policy.LayerCode（再巷） | Policy.AisleCode |
| 主数据 | 用 **Layer 表** | 通常不用 Layer 实体 |
| 图节点 | **无独立 Node 表**，`Location` 即节点 | 同左（BasicData 共用） |

---

## 4. 入库交接与设备流程（摘要）

```text
WMS 组盘 → StorageTask4Shuttle (Create；可带 LayerCode/AisleCode)
  → 设备目的地申请 ShuttleExecuteStack.DestinationRequest
       · 判重写 DestinationRequest
       · RequestPoint 类型（如 LocationRequest）→ LocationRequest
       · 内调分配：层/巷/位，更新 DesAddress、Status（AisleAssigned/LocationAssigned）
  → TransportTask + 路径（LesRoute / 按层 MAP）
  → ShuttleTask / ShuttleExecTask(+Path) 与/或 HoistExecTask（跨层）
  → 完成回写 StorageTask → WMS 落位
```

出库：Retrieval 体系 + 穿梭/提升调度（深浅、停车账本 `ShuttleParkingLedger`、派车 `ShuttleDispatch`）；路径与流量在 WCS 侧，不进 WMS 账本。

实体要点：

| 实体 | 作用 |
|------|------|
| StorageTask4Shuttle | 入库锚点 + Layer/Aisle |
| ShuttleTask | 调度意图：取放层/巷/址、车号、优先级 |
| ShuttleExecTask + Path | 可执行段与节点序列 |
| Hoist* | 跨层提升 |
| TransportTask / DeviceExecTask / LesRoute* | 通用搬运与路网 |

---

## 5. 路径与交通

- 路网：`LesRoute` / `LesRouteFlow`，常按 **层 MAP** 隔离。  
- 停车：`ShuttleParkingLedger` 统一预订/停靠/释放。  
- 热路径：项目内还有仿真/网关；迁 Seven 时对应 **Fw_ + HotStore(`fw:`)**，见现有 FourWay 骨架。

---

## 6. 与 Seven 已有 FourWay 骨架的关系

Seven 已有 `Fw_MapVersion` / `Fw_Node` / `Fw_Route` / `Fw_ShuttleTask` — 对应 RCS 的地图节点边与 Shuttle 任务，**尚未**对齐：

- 一等公民 **Layer** 主数据  
- SelectLayer→Aisle→Location 全链  
- StorageTask4Shuttle / Hoist / 停车账本  

迁移见 [02-migration-plan.md](./02-migration-plan.md)。

---

## 7. 关键绝对路径

```
LES.Entities\Models\BasicData\{Zone,Layer,Aisle,Location,AssignmentPolicy}.cs
LES.Entities\Models\WMS\Inbound\StorageTask4Shuttle.cs
LES.Entities\Models\WCS\Shuttle\{ShuttleTask,ShuttleExecTask,ShuttleExecTaskPath,ShuttleDevice}.cs
LES.Entities\Models\WCS\Hoist\*.cs
LES.BLL\Service\ExecuteStack\BasicStack\Location\LocationExecuteStack.AllocationEngine4Shuttle.cs
LES.BLL\Service\ExecuteStack\BasicStack\Location\LocationExecuteStack.AllocationEngine4SRM.cs
LES.BLL\Service\ExecuteStack\WCS_Stack\DeviceStack\Shuttle\ShuttleExecuteStack.cs
```
