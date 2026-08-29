# Seven 堆垛机（Stacker）WCS 包迁移方案

状态：WCS-S1（申请/分配）+ WCS-S2（寻路拆腿）已落地；P3 真机待实施  
日期：2026-08-29  
货位前缀：见 [`../wms/02-location-multi-pack-prefix.md`](../wms/02-location-multi-pack-prefix.md)（**Stk.***）  
产品文档：[`../../doc/20-WMS与WCS实现说明.md`](../../doc/20-WMS与WCS实现说明.md)（第二部分：立库 WCS）

---

## 1. 职责切分（硬边界）

```text
WMS
  · 账本、单据、拣选预约
  · 库位主数据 Code=Stk.* + PackId=stacker
  · 不实现 SelectAisle 轮转细则

Bus
  · TransportOrder / Leg 生命周期
  · 跨包交接（Stk↔Fw）
  · 触发 WMS 落账
  · 不存 LesRoute 流量、不做 SUDR

Stacker Pack（本方案）
  · RequestPoint / DestinationRequest 语义
  · AssignmentPolicy + 巷道/货位分配
  · PutAway / Retrieval 包内任务
  · 包内路径与 DeviceTask（可分期）
  · IEquipmentTriggerPort：SUDR/SUDS/段反馈
```

---

## 2. 表与前缀

| 表 | 说明 |
|----|------|
| `Wms_*` | `Stk.` 前缀库区/巷道/货位；权威占用与库存 |
| `Stk_RequestPoint` | 申请点；ZoneCodes 仅含 `Stk.*` |
| `Stk_AssignmentPolicy` / `Stk_AssignmentRecord` | 高重、Next 链、轮转 |
| `Stk_PutAwayTask` | ≈ PutOn/Storage；关联 Bus Leg / 容器 |
| `Stk_RetrievalTask`（**补**） | ≈ PutOff/Retrieval；WcsGroupNo/WcsPri |
| `Stk_DeviceTask` | ≈ DeviceExecTask |
| `Stk_Route` / `Stk_RouteFlow`（P2） | 包内边与流量 |
| `Stk_DeviceCoder` | 设备地址 ↔ `Stk.*` LocationCode |

---

## 3. 接口契约（包内）

```text
IStackerDestinationService
  OnSudr(trigger)           // 分流 Aisle/Location/Block/Reject
  Reapply(requestId)

IStackerAisleAllocator / IStackerLocationAllocator
  // 对齐 LES SelectAisle / SelectLocation（P0 补齐规则）

IStackerWcsPack : IWcsPack
  AcceptLeg(leg)            // 入→PutAway；出→Retrieval
  OnDeviceSegment(...)      // 推进 DeviceTask / 完成 Leg

IWcsLocationSchema (PackId=stacker)
  层级：Zone → Aisle → Location(Row,Col,Layer,Depth)
  NormalizeCode → 强制 Stk. 前缀
```

---

## 4. 与总线的时序

### 4.1 入库

```text
WMS Receive/组盘（库存已入账，Location 可为收货位）
  → Bus.Create(From=收货位, To=待定或巷道口, Pack=stacker)
  → AcceptLeg → Stk_PutAwayTask(Create)
  → SUDR Aisle/Location 更新任务 Des + Wms_Location 预约
  → DeviceTask… → Completed
  → Bus Leg Complete → WMS 位置落到 Stk. 货位
```

### 4.2 出库

```text
WMS Assign + Picking → Bus.Create(From=Stk.货位, To=出库口/月台)
  → AcceptLeg → Stk_RetrievalTask
  → （可选）TransferBin 子任务
  → DeviceTask… → Completed → WMS 扣账/拣选确认
  → 同组滚动下一 Pri（包内调度器或轻量领域服务）
```

---

## 5. 阶段（相对当前 Seven 骨架）

| 优先级 | 内容 | 现状差距 |
|--------|------|----------|
| **P0** | LocationCode 全面 `Stk.` + Schema 校验 | ✅ M1：`IWcsLocationSchema` + LocationService 前缀拒绝；Deploy 写 `Stk.` |
| **P0** | Allocator 对齐 LES：高重、轮转、EpPoint、空位门槛、Booking/LockBin、双深 | ✅ **WCS-S1**：高重/权重轮转/空位门槛/浅深+Booking；✅ **WCS-S3**：双深 Bin 组/LockBin |
| **P0** | SUDR 拒收 / 无任务 / 模式冲突 → RejectAddress | ✅ **WCS-S1**：`RejectDestination`（外形 NG/无点/无任务/分配失败）；联锁拒接单见 Platform |
| **P1** | 二次申请（巷口→货位）与段反馈再触发 | ✅ **WCS-S1**：LocationRequest + BlockingPoint 重分配；段反馈完成仍一次结案（多段路径 S2） |
| **P1** | `Stk_RetrievalTask` + 出库 AcceptLeg + 深浅移库 | ✅ M3 竖切；✅ **WCS-S3**：深浅移库 TransferBin |
| **P1** | WcsGroup/Pri 滚动 | ✅ M3 同组最小 Pri 下发 |
| **P2** | `Stk_Route` + 流量拆腿（SupperRoute 概念） | ✅ **WCS-S2**：Dijkstra + 同 ExeStack 合并 + Flow；多种子/时间窗后置 |
| **P2** | 阻塞点重分配、盘点 Type 分支 | BlockingPoint 重分配见 S1；✅ **WCS-S3**：盘点锁仓跳过巷道 |
| **P3** | 真机 DeviceComm/S7 替代 InMemory TriggerPort | Phase H |

---

## 6. 测试验收

1. **分配**：给定策略链与双深货位，SelectAisle/Location 结果与 LES 用例表一致（金样例）。  
2. **入库 E2E**：组盘 → SUDR → 两阶段目的地 → 完成 → `Wms_Stock.LocationCode=Stk.*`。✅ M2（`BuildPallet` + Allocator + Detail）  
3. **出库 E2E**：预约 → Retrieval →（阻挡移库）→ 完成 → 滚动下一 Pri。✅ M3；✅ **WCS-S3** 深浅移库  
4. **多包共存**：同仓 `Stk.*` 与 `Fw.*`；交接仅经 HandoverLink；Stacker 分配永不选中 `Fw.` 码。  
5. **前缀**：写入无前缀码必须失败。

---

## 7. 实施顺序建议

1. 落地前缀 + `IWcsLocationSchema`（与 wms M1 同步）。  
2. 强化 Allocator + DestinationService 拒收（P0）。  
3. 补 Retrieval 任务与出库 E2E（P1）。  
4. 路径流量（P2）——在申请分配稳定后进行。  

不复制 LESWebVM/LES2 程序集；只迁规则与状态机。
