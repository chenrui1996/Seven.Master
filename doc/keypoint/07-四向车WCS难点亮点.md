# 07 · 四向车 WCS：要点、难点、亮点

PackId=`fourway`；表 `Fw_*`；货位 `Fw.*`。  
入口：`FourWayWcsPack`；`FourWayDestinationService`；宿主：`FourWaySchedulerHostedService`（SUDR + 周期：Path 重试 / 停车唤醒 / Hoist 补扫）。  
热路径命名空间：`fw:`。

详见 [doc/20 第三部分](../20-WMS与WCS实现说明.md)。

---

## 1. 与立库的本质差异（先讲清楚）

| 维度 | 堆垛 | 四向 |
|------|------|------|
| 空间 | 巷道 + 堆垛机主导 | **层** 为强边界；层内多车 |
| 分配 | 巷→位 | **层→巷→位** |
| 交通 | RouteFlow / 设备段 | 层内边占用（HotStore）+ Parking |
| 跨层 | 通常单巷垂直 | **Hoist 提升机** 三阶段 |
| 热数据 | 依赖较少 | **强依赖** `fw:` |

---

## 2. 任务与表

| 对象 | 用途 |
|------|------|
| `Fw_LayerPolicy` / `AislePolicy` / `AssignmentRecord` | 层巷策略与轮转 |
| `Fw_PutAwayTask` / `RetrievalTask` | 入出库任务 |
| `Fw_ShuttleTask` / `ShuttleTaskPath` | 穿梭车载体与路径点列 |
| `Fw_RequestPoint` | Layer/Aisle/Location；Hoist/Shuttle 口类型（SUDR 勿误拒） |
| `Fw_MapVersion`(含 LayerCode) / Node / Route | **层内**路网 |
| `Fw_ParkingLedger` | Free / Reserved / Occupied |
| `Fw_HoistDevice` / LayerPoint / Task / ExecTask | 提升机 |

`CanHandle`：两端 `Fw.` 或交接；纯 `Stk.*` 拒接。

---

## 3. 入库分配（三阶段）

```text
FourWayInboundAllocator Booking
  → AcceptLeg：PutAway + Shuttle(Accepted)
  → SUDR Layer/AisleRequest → 选层巷 → Dispatch(Ep)
  → LocationRequest → Book → Dispatch(Bin)
  → Path + 段反馈 → Completed → Bus → WMS
```

### Select 要点

- **层**：策略可用、对齐 `Wms_Layer`、权重+轮转  
- **巷**：同层；`MinEmptySlots` **按本层**计；`MaxShuttleCount`：该层巷 `Reserved+Occupied` 达上限则跳过  
- **位**：同仓同巷空闲 + `IsBooked`

**难点话术**：MaxShuttle 把「巷道车辆密度」做成分配约束，避免层内堵死。

---

## 4. 难点 A：层内寻路 + 交通占边

```text
PathDispatcher
  → ResolveMapVersionId：显式 Id → LayerCode → Active → 首条
  → FourWayRouter Dijkstra → 写 ShuttleTaskPath
  → TrafficGuard.TryGrant 首跳 → Dispatch
SegmentFeedback → Advance：释边 → 占下一段
无 Map/无边 → 单段 From→To
```

### 硬化点（FFU）

- `Routing` 占边失败：**保持状态**，周期重试（不是直接 Failed 打爆）
- Cancel/Fail：**释尽**该车边（path EdgeId + owner 索引；HotStore `fw:flow:`）
- 生产禁用 Demo `wcs:` 键

### 面试对比 Redis 锁

TrafficGuard 是**路径段级授予**，与整单锁不同；配合反馈推进，形成「一步一授予」的交通管制。

---

## 5. 难点 B：出库停车位（Parking）

```text
AcceptRetrieval → TryDispatch（Pri 门闩）
  → 原子预订：UPDATE ParkingLedger SET Reserved WHERE Status=Free …
  → PathDispatcher；仅 shuttle Running 后 → Occupied + Retrieval Dispatched
  → 占边失败：保持 Routing/Reserved（可重试）
  → 无空闲车位 → Suspended；周期发现 Free 再唤醒
段完成 → 释停车 → 下一 Pri
```

**为什么要 Free→Reserved→Occupied 三态？**

- Free：可抢  
- Reserved：已分给任务但车未真正占用（防两单抢同一车位）  
- Occupied：执行中实占  

原子 `WHERE Status=Free` 是**并发下的正确性核心**（数据库条件更新），面试要主动提。

---

## 6. 难点 C：提升机跨层（Hoist）——最大亮点之一

### 策略决策

**单 Bus Leg + 包内三阶段**，不把跨层拆成多段总线。

原因：

- 总线保持「一次搬运意图」简单  
- 跨层联锁、排队、失败重试是包内领域知识  
- WMS 仍只看到一次运输完成

### 三阶段

```text
AcceptLeg 发现 From/To 不同层
  → 选口（Available 机 + 空闲口；出库优先 Outbound Ap/Ep）
  → HoistTask + Exec(Queued) + Shuttle
  → ① ToSrcAp：货位 → 源层 Hoist AP
  → ② HoistLift：同口排队（Pri/时间）；Dispatch 到目标层 EP
  → ③ FromDesEp：目标层 EP → 目标货位
  → Completed → Bus → WMS
```

### 联锁

- 同口已有 Dispatched → 后到 **Suspended**  
- 完成/失败/周期补扫 → 唤醒队列  
- `FeedbackCode≠OK`：不盲目推进（抬升段可 Failed；到 AP/目标可保持重试）  
- 段反馈优先 `FourWayHoistOrchestrator` 消费

### 面试金句

> 跨层是「设备域编排」，不是「再开一张运输单」。我们把复杂度收在 HoistOrchestrator，总线和 WMS 契约保持稳定。

---

## 7. 双包静默与申请点类型

- 外形 NG 且点属启用堆垛申请点 → 四向侧静默  
- Hoist/Shuttle 口类型的 RequestPoint：**SUDR 分配不要误当成普通巷道申请而 Reject**

---

## 8. 关键类型速查

| 类 | 职责 |
|----|------|
| `FourWayInboundAllocator` | 三阶段分配 |
| `FourWayDestinationService` | SUDR / 段反馈 |
| `FourWayPathDispatcher` / `Router` / `TrafficGuard` | 寻路占边 |
| `FourWayHoistOrchestrator` | 跨层三阶段 |
| `FourWayWcsPack` | 接单 / Retrieval / 停车 / CanHandle |
| `FourWaySchedulerHostedService` | 周期重试与唤醒 |

路径：`Infrastructure/Wcs/Packs/FourWay/`。

---

## 9. 亮点总结

1. 层作为一等公民：MapVersion 带 LayerCode，分配按层约束。  
2. HotStore `fw:` + TrafficGuard 支撑多车层内交通。  
3. 停车三态 + 条件更新解决并发抢位。  
4. Hoist 单 Leg 三阶段：边界清晰、可测、可补扫。  
5. Scheduler 把「瞬时失败」变成「可恢复状态机」，贴近现场。

---

## 10. 已知延后

- F6：仿真 Promote / Simulator 产品化闭环  
- Phase H：DeviceComm 真机 Port  
- HotStore 多实例边索引强一致增强  
- 部分关系库并发预订单测（曾 defer）
