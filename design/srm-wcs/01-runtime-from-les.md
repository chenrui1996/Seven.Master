# 堆垛机 WCS 运行时：LES2 + LESWebVM 对照

状态：调研完成  
日期：2026-08-29  

源码：

- LES2：`D:\Junheinrich\Project\FJD\SrcCode\LES2`
- LESWebVM：`D:\Logbot\1 Project\Projects_2020\LB-SS141A_BPS\trunk\SourceCode\LESWebVM`

---

## 1. 两套命名对照

| 语义 | LES2 | LESWebVM |
|------|------|----------|
| 入库执行锚点 | **StorageTask** | **PutOnTask** |
| 出库执行锚点 | **RetrievalTask** (+ RetrievalExecuteTask) | **PutOffTask** (+ PullOffExecuteTask) |
| 申请流水 | DestinationRequest | DestinationRequest |
| 申请点 | RequestPoint | RequestPoint |
| 输送/搬运单 | TransportTask | TransportTask |
| 设备腿 | DeviceExecTask | DeviceExecTask |
| 路径边/流量 | LesRoute / LesRouteFlow | 同左 + SupperRoute |
| 巷道/货位分配 | LocationExecuteStack（LES2 侧同类） | `LocationExecuteStack.SelectAisle/SelectLocation` |

Seven 映射：业务锚点 → **Bus_TransportOrder + Leg**；包内 → **Stk_PutAwayTask / Stk_Retrieval*(待建) / Stk_DeviceTask**；路径仅 **Stk_**（或包内 Route 表）。

---

## 2. 入库主链（设备视角）

```text
SUDR（外形检 + 托盘 + 源地址 + 高/重）
  → 防抖写 DestinationRequest
  → DeviceCoder：设备地址 → 位置码 → RequestPoint
  → 非 OK → 拒收口 TransportTask
  → AisleRequest：SelectAisle → PutOn/Storage.DesAddress = 巷道 EpPoint，AisleAssigned
  → LocationRequest：SelectLocation → DesAddress = 货位，LocationAssigned，Booking/LockBin
  → Create TransportTask(申请点 → Des)
  → RouteMaster.SelectRoute → SupperRoute.FindRoute
  → CreateTasks：按 ExeStackCode 切 DeviceExecTask，占 LesRouteFlow
  → SUDS 下发；SUPR 到站释流；可能再次 LocationRequest
  → Finish → 完成 PutOn/Storage → 回写 WMS
```

### 2.1 申请点类型（核心）

| Type | 含义 |
|------|------|
| AisleRequest (10) | 巷道分配 |
| LocationRequest (20) | 货位分配 |
| BlockingPointRequest (30) | 阻塞重分配（终止在途再派） |

### 2.2 分配要点（必须迁入 Stacker Allocator）

**巷道 SelectAisle**

- 策略表：高/重上限、Next 正反向链、`StartSign`、分类 `ClassifyFlag` 轮转
- 巷道：可入、非盘点锁、空位数量门槛（双深更严）
- 结果：EpPoint；写 AssignmentRecord

**货位 SelectLocation**

- 同巷道过滤高重、空闲、Bin
- 双深：BinCode 组、浅深优先规则、孤二深排除
- 排序：Height、Deepth、同类货、Load、ABC、层列排
- 预约 BookingFlag；同 Bin LockBin

---

## 3. 出库主链

```text
WMS 建 Retrieval/PutOff（含 WcsGroupNo/WcsPri）
  → 一般不经 SUDR
  → 深浅干涉：阻挡托 TransferBin（先移库再出）
  → TransportTask(Outbound) → 寻路拆腿 → SRM/输送
  → Finish → 拣选确认 / 滚动同组下一 WcsPri
```

LES2 额外：`RetrievalExecuteTask` 按巷道滚动；`SpecialWcsGroupNo="1"` 紧急不受同组限制。

---

## 4. 路径拆腿（包内）

| 组件 | 职责 |
|------|------|
| LesRoute | 边：MAP、ExeStackCode、起终点、容量、权重、动态流量 |
| SupperRoute | 多种子并行搜路，流向冲突与动态流量时间窗 |
| CreateTasks | 连续同 ExeStackCode 合并为一段 DeviceExecTask；首段立即、后续 Event(EP) |
| ReleaseRoute | SUPR 释流；终点 FinishTransportTask |

**JudgeMap**：Bin 起终点常映射到同巷道 Srm 点再寻路。

---

## 5. LES2 `Models\WCS` 目录注意

该文件夹**混有**申请/执行器与**输送路径**实体；Storage/Retrieval **不在**此文件夹（在 WMS 下）。  
迁移时按职责拆到 Seven：`Stk_` 申请分配设备 + 可选 `Stk_Route*`，不要整目录照搬表名。

---

## 6. 关键路径

**LES2**

```
LES.Entities\Models\WCS\*.cs
LES.Entities\Models\WMS\Inbound\StorageTask.cs
LES.Entities\Models\WMS\Outbound\RetrievalTask.cs
LES.BLL\Service\ExtendService\WCS\*
LES.BLL\Service\ExecuteStack\WCS_Stack\InboundExecuteStack.cs
LES.BLL\Service\ExecuteStack\WCS_Stack\DeviceStack\SRMExecuteStack.cs
```

**LESWebVM**

```
LES.BLL\Service\ExecuteStack\WCS_Stack\DeviceStack\PalletConveyorExecuteStack.cs
LES.BLL\Service\ExecuteStack\WM_BLL_Stack\Basic\LocationExecuteStack.cs
LES.BLL\Service\ExecuteStack\RouteStack\RouteMasterExecuteStack.cs
LES.BLL\Service\ExecuteStack\RouteStack\RouteSubExecuteStack.cs
LES.Route\SupperRoute.cs
```
