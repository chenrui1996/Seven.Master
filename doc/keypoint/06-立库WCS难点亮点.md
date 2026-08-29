# 06 · 立库 WCS：要点、难点、亮点

PackId=`stacker`；表 `Stk_*`；货位 `Stk.*`。  
入口：`StackerWcsPack`；目的地状态机：`StackerDestinationService`；宿主：`StackerSchedulerHostedService`。

详细步骤见 [doc/20 第二部分](../20-WMS与WCS实现说明.md)。本文偏**面试深挖**。

---

## 1. 任务对象心智模型

| 对象 | 一句话 |
|------|--------|
| `Stk_PutAwayTask` | 入库上架，与 Leg 1:1 |
| `Stk_RetrievalTask` | 出库取货；带 `WcsGroupNo` / `WcsPri` |
| `Stk_DeviceTask` | 设备段；`Seq`、`ExeStackCode`、From/Dest |
| `Stk_RequestPoint` | 申请点（巷道/货位/阻挡点…） |
| `Stk_AssignmentPolicy/Record` | 巷道策略与轮转记录 |
| `Stk_Route` / `RouteFlow` / `DeviceCoder` | 路网、占边流、点码映射 |
| `Stk_LocationProfile` | 双深组、LockBin 等外形配置 |

入库 Ref → PutAway；出库 `OutboundOrder` / 内部 `StackerTransfer` → Retrieval。

---

## 2. SUDR 入库分配（要点）

```text
Bus → PutAway(Accepted)
  → DestinationRequest @ AisleRequest
       NG → Reject + PutAway Failed
       OK → SelectAisle → AisleAssigned → Dispatch(Ep) + DeviceTask
  → （可选）LocationRequest → SelectLocation + Book → Dispatch(Bin)
  → （可选）BlockingPoint：作废在途段、释预约、重选
  → SegmentFeedback → 推进 / Completed → Bus → WMS
```

### SelectAisle 排序逻辑（可背）

过滤：策略可用、高重、巷道可用、盘点锁仓跳过、`MinEmptySlots`…  
排序：**AllocationWeight ↓ → LastAssignedAt ↑ → AisleCode**（权重优先 + 轮转公平）。

### SelectLocation（含双深）

- 同巷、`PackId=stacker`、空闲
- 排除 InLockBin
- **深位仅当同组更浅已占用或已预约**（防孤二深）
- 排序：Depth → Layer → Column → Row → Code
- Booking：目标 `IsBooked`；同 `BinGroupCode` 其它空位一并预约

---

## 3. 难点 A：双深货位（Double Deep）

### 问题本质

双深巷道：浅位（Depth=1）挡住深位（Depth=2）。若先把货放进「孤深位」，浅位空着，堆垛机取深位会物理干涉或策略崩溃。

### 解法分层

1. **分配期**：`StackerDoubleDeepRules` —— 不允许选出孤深；Booking 时把同组相关空位一起预约，防止别人插入浅位破坏几何。
2. **出库期**：见难点 B（DepthGuard）。
3. **配置**：`Stk_LocationProfile`（BinGroup / LockBin）；盘点锁等与仓级锁配合。

### 面试话术

> 双深不是简单「深度字段排序」，而是**几何约束 + 预约一致性**。我们在分配规则和出库挡路处理两条线同时保证。

---

## 4. 难点 B：深浅移库（DepthGuard）

### 场景

要出深位货，但浅位有货挡路。

### 算法（口述）

```text
TryDispatch 深位 Retrieval
  → DepthGuard 发现被浅位挡
       · 深位任务 Suspended，WcsPri + 1（让路）
       · 建 Bus RefType=StackerTransfer：浅位 → 同巷空位（用原 Pri）
  → 先执行 Transfer
  → 完成后滚动 Pri，再派原深位
```

完成态：移库存、释源占目标；与正常出库完成路径对齐。

**亮点**：用**同一套 Retrieval + 总线**表达「辅助移库」，而不是硬编码设备特殊指令——业务状态机可测。

---

## 5. 难点 C：寻路与拆腿（PathDispatcher）

```text
dest 点码
  → DeviceCoder 判是否在图
  → Dijkstra（满容量边不可用）
  → 连续相同 ExeStackCode 合并为一段 DeviceTask
  → 写 RouteFlow；仅首段 Dispatched
SegmentFeedback → 释流 → 下一段 Dispatched → …
无路网数据 → 退化为单段（兼容「只做分配」阶段）
```

### 为什么要「合并同 ExeStackCode」？

堆垛/输送可能在同一执行栈连续多跳；合并减少设备任务碎片，贴近现场「一段行程」。

### 出库同样走 PathDispatcher

From→To 与入库对称，避免两套路径引擎。

---

## 6. 出库 Pri 滚动（要点）

同组更小 Pri 未终态 → 当前 **Suspended**；段完成再 `TryDispatch` 下一 Pri。  
与四向停车/跨层不同，立库核心是**顺序门闩 + 可选深浅移库**。

---

## 7. 双包 Trigger 静默（硬化）

共享 `IEquipmentTriggerPort`：若 SourcePoint 是启用中的**四向**申请点且外形 NG，堆垛侧**静默**不 Reject——避免误伤四向联调。

---

## 8. 关键类型速查（面试点名）

| 类 | 职责 |
|----|------|
| `StackerAisleAllocator` / `LocationAllocator` | 分配 |
| `StackerDoubleDeepRules` / `DepthGuard` | 双深 / 挡路移库 |
| `StackerRouter` / `PathDispatcher` | 寻路拆腿 |
| `StackerDestinationService` | SUDR / 段反馈状态机 |
| `StackerInboundAllocator` | WMS 组盘入口 |
| `StackerWcsPack` | 接单契约 |

路径均在：`Infrastructure/Wcs/Packs/Stacker/`。

---

## 9. 亮点总结（可直接念）

1. SUDR 把「申请目的地」从业务里抽成设备语义，仿真与真机同构。  
2. 双深用规则 + Booking 组约束，而不是事后人工改库。  
3. 深浅移库升 Pri + Transfer 任务，复用 Retrieval 管道。  
4. Dijkstra 拆腿 + ExeStackCode 合并，贴近真实设备段。  
5. 无图退化单段，保证分配阶段可独立交付。

---

## 10. 已知延后

- SupperRoute 多种子与时间窗  
- OutLockBin 更强硬拒绝策略  
- Phase H 真机 Port  
