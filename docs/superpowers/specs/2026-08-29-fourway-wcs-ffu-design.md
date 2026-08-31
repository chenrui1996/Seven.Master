# 四向车 WCS Follow-up 硬化（WCS-FFU）

状态：已落地（2026-08-29）  
日期：2026-08-29  
依据：F2–F5 终审台账；对话锁定：全量台账（档 2）、节奏 A（按子系统竖切）  
前置：[`2026-08-29-fourway-wcs-f2-f5-design.md`](./2026-08-29-fourway-wcs-f2-f5-design.md) 已落地  

不含：F6 Simulator Promote、真机 DeviceComm。

---

## 1. 目标

消除 F2–F5 终审列出的 Important/关键 Minor，使同仓双包、层内占边、停车容量、Hoist 队列在仿真与重启场景下可运维。

## 2. 交付节奏（方案 A）

| 竖切 | 代号 | 产品文档 |
|------|------|----------|
| 双包 Trigger | **FU1** | 记入 `doc/27` |
| Path 硬化 | **FU2** | 回写 `doc/25` + `doc/27` |
| Parking / MaxShuttle | **FU3** | 回写 `doc/25` + `doc/27` |
| Hoist 硬化 | **FU4** | 回写 `doc/26` + `doc/27` |

收口文档：`doc/27-WCS四向硬化与跟进.md`；更新 `doc/19`/`20`/`README`、`design/shuttle-wcs/02`。

---

## 3. FU1 — 双包 Trigger

### 行为

- **Stacker**：`CheckResult≠OK` 时，若 `SourcePointCode` 命中启用 `Fw_RequestPoint` → **静默 return**（不 Reject）。未知点且非 Fw → 仍可 Reject（保持现逻辑）。
- **FourWay**：对称——NG 时若命中启用 `Stk_RequestPoint` → 静默。
- 清理 DualPack 测试中无用的 `WcsPackResolver` 死代码。

### 验收

- 双 Dest 订阅：Fw RP + NG → 仅 FourWay 记 Reject；Stk RP + NG → 仅 Stacker 记 Reject。
- 既有 OK 路径 / 双包静默忽略未知对端 RP 行为不回归。

---

## 4. FU2 — Path

### 行为

| 项 | 设计 |
|----|------|
| Grant 重试 | HostedService（可并入 `FourWaySchedulerHostedService`）周期扫描 `Fw_ShuttleTask` 为 `Routing` 且首跳未占边成功的任务，重试 `TryGrant` + Dispatch |
| Cancel 释边 | `CancelLeg` / PutAway·Retrieval Failed·Cancelled 时调用 TrafficGuard `Release` 清理该任务占用的边 |
| Layer↔Map | `Fw_MapVersion` 增加 `LayerCode`（可空）；`ResolveMapVersionId`：优先匹配层码 → 再 Active → 再首条 |

### 验收

- 占边失败后经重试可下发。
- 取消后无残留 `fw:flow:`（或进程内 fallback 键）。
- 两层两图：路径边只来自本层 MapVersion。

---

## 5. FU3 — Parking / MaxShuttle

### 行为

| 项 | 设计 |
|----|------|
| 并发预订 | `TryReserveParking` 用事务 + 行级选取（先查 Free 再条件更新 `WHERE Status=Free`）；失败则挂起 |
| Occupied | 段开始（首段 Dispatched 成功）或到站反馈时 `Reserved→Occupied`；完成/取消 → `Free` |
| 挂起唤醒 | HostedService 扫描 `Suspended` Retrieval（因无停车），有 Free 位则 `TryDispatchRetrieval` |
| MaxShuttleCount | Allocator 选巷时：该层该巷 `Reserved+Occupied`（及可选在途 Retrieval）≥ MaxShuttleCount 则跳过 |

### 验收

- 并发双订不双占同一车位。
- 无位挂起 → 释放/补位后可再派。
- 超 MaxShuttle 的巷不被自动选中（有空测例）。

---

## 6. FU4 — Hoist

### 行为

| 项 | 设计 |
|----|------|
| 补扫 | HostedService：启动 + 周期 `TryDispatchQueuedForPort` / 唤醒 `Suspended` Exec |
| SUDR 类型 | `FourWayDestinationService` 对 HoistInbound/Outbound EP·AP、Shuttle EP·AP：至少 **不误 Reject**；能完成口状态推进的接 Dispatch/完成（与现有 SegmentFeedback 编排一致即可，不要求完整 RCS 协议） |
| 多机选型 | 同层多 `Fw_HoistLayerPoint`：优先 `Fw_HoistDevice.IsAvailable` + 口空闲（无 Dispatched 同口 Exec） |
| 出入库口 | 按 Bus/Leg `RefType`：出库类用 Outbound Ap/Ep，其它用 Inbound；空则交叉回退 |
| 错误反馈 | `FeedbackCode≠OK` 时不推进 Hoist 下一阶段；标记 Failed 或保持当前阶段可重试 |

### 验收

- 模拟「仅有 Suspended Exec」时补扫可 Dispatched。
- 出库跨层使用 Outbound 口（有测例）。
- 同口联锁保持；错误段反馈不进入抬升/下一阶段。

---

## 7. 明确不做

- F6 Promote / 真 PLC / DeviceComm
- SupperRoute 多种子与时间窗
- 重写 F2–F5 主路径（仅硬化）

---

## 8. 实现顺序

1. FU1 → 单测双包 NG  
2. FU2 → 迁移 LayerCode + 重试/释边测  
3. FU3 → 并发/Occupied/MaxShuttle 测  
4. FU4 → 补扫/口分支/错误反馈测 + doc/27  

用户批准本 spec 后写 `docs/superpowers/plans/2026-08-29-fourway-wcs-ffu.md` 并开工。
