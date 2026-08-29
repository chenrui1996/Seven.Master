# WCS-S1：堆垛机申请与分配对齐 LES

状态：已批准实施用户选 B）  
日期：2026-08-29  

## 目标

在现有 `StackerDestinationService` + Allocator 骨架上，对齐 LES **SUDR 申请语义**与 **SelectAisle / SelectLocation 核心规则**（可验收竖切）。  
**不包含**：`Stk_Route` 多段寻路（S2）、真机 DeviceComm（Phase H）、四向包。

## 范围

| 做 | 不做 |
|----|------|
| CheckResult≠OK / 无点 / 无任务 / 分配失败 → RejectDestination | SupperRoute / LesRouteFlow |
| AisleRequest → SelectAisle（高重、可用、空位门槛、权重轮转） | Next 正反向链全量（可后置） |
| LocationRequest → SelectLocation（空闲、浅深优先、Booking） | 双深 BinCode 组、孤二深完整 LES 规则 |
| BlockingPoint：取消在途 DeviceTask 后重分配货位 | 盘点锁全分支、深浅移库出库 |

## 设计

```text
SUDR (IEquipmentTriggerPort.DestinationRequested)
  → StackerDestinationService
       ├─ !OK / 未知点 / 无 PutAway → RejectDestination
       ├─ AisleRequest → StackerAisleAllocator → DispatchDestination(EpPoint)
       ├─ LocationRequest → StackerLocationAllocator(+Book) → DispatchDestination(Bin)
       └─ BlockingPoint → 取消未完成 DeviceTask → 再 Location/Aisle
SegmentFeedback → 完成 Device/PutAway → Bus.OnLegEvent(Completed)
```

### 策略字段增量

`Stk_AssignmentPolicy` 增加 `MinEmptySlots`（默认 1）、`AllocationWeight`（默认 1）。  
巷道可用另读 `Wms_Aisle.IsAvailable`（若存在同 Code 行）。

### 端口增量

`IEquipmentTriggerPort.RejectDestinationAsync(RejectDestinationCommand)` — 仿真端口记录列表，供联调/测试。

## 验收

1. 单测：拒收外形 NG；无任务拒收；空位不足跳过巷道；浅深优先并 IsBooked。  
2. 既有 `StackerPackTests` / 入库 E2E 仍通过。  
3. `doc/21` + `doc/20` 标明 S1 已落地、S2/S3 未做。
