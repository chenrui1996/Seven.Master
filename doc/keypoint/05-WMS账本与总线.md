# 05 · WMS 账本与编排总线

## 1. 职责边界（先画清）

| 层 | 表前缀 | 职责 | 不做什么 |
|----|--------|------|----------|
| WMS | `Wms_` | 主数据、库存真相、入/出/盘点意图 | 不跑设备寻路、不占边 |
| Bus | `Bus_` | 「容器从 A 到 B」的跨包编排 | 不合并各包路网 |
| Pack | `Stk_` / `Fw_` | 接 Leg、分配、寻路、设备段 | **禁止第二套可扣减库存表** |

完成回调：`WmsTransportCompletionHandler` —— 唯一把运输结果写回 `Wms_Stock` / 单据的权威路径之一。

---

## 2. 三层单据模型（核心亮点）

```text
L1 业务单   Wms_InboundOrder / OutboundOrder / 盘点计划
L2 运输单   Bus_TransportOrder（容器 A→B，可跨包）
L3 腿+包任务 Bus_TransportLeg(PackId) → PutAway/Retrieval/Shuttle…
              → DeviceTask / ShuttleTaskPath / HoistExec…
```

**为什么拆三层？**

- L1 面向仓管/ERP：数量、审核、完成态
- L2 面向跨设备族搬运意图
- L3 面向单包执行与设备节拍

WMS 提交时只带 From/To/容器/业务引用；**不**传设备路径。

---

## 3. 一仓多包与编码前缀

| PackId | 货位前缀 | 表前缀 |
|--------|----------|--------|
| `stacker` | `Stk.` | `Stk_` |
| `fourway` | `Fw.` | `Fw_` |
| `boxsort` | `Bs.` | 预留 |

- `Wms_Warehouse.EnabledPackIds`：逗号分隔，至少一种
- Zone / Layer / Aisle / Location **冗余 PackId**
- `PackCodeRules` + `IWcsLocationSchema`：拒绝前缀与 Pack 不一致
- 跨包交接：`Wms_HandoverLink`（FromPack→ToPack + LocationCode）
- 盘点锁仓：`IsCycleCountLocked`（立库分配跳过）

**亮点话术**：统一骨架表 + 前缀命名空间，同仓堆垛与四向共存，库存仍只有一套 `Wms_Stock`。

### 层级

```text
Warehouse → Zone(PackId) → [Layer] → Aisle(PackId) → Location(PackId)
```

四向重度用 Layer；堆垛可弱化 Layer。

### 分配入口

| 接口 | 作用 |
|------|------|
| `IWcsLocationAllocator` | `AllocateInboundAsync` |
| Resolver | 按 PackId 选实现 |

- 堆垛：巷 → 位（`StackerInboundAllocator`）
- 四向：层 → 巷 → 位（`FourWayInboundAllocator`）

策略参数在包表（`Stk_AssignmentPolicy` / `Fw_LayerPolicy`…），**WMS 无总策略表**——避免中央上帝对象。

---

## 4. 库存与容器（一致性要点）

| 能力 | 实现要点 |
|------|----------|
| 收货 | `StockService.ReceiveAsync`（可选 Ledger） |
| 发运 | `ShipAsync` |
| 出库审核 | 常先扣 **AvailableQty**（预留），运输完成再正式 Ship |
| 容器 | `Wms_Container.LocationCode` ↔ 货位 `CurrentContainerCode` / `IsOccupied` / `IsBooked` |

运输完成处理器常见动作：

- 移库存 From→To
- 占目标位；出库 / `StackerTransfer` 释放源位
- 按 `RefType` 回写 Detail / 出库单完成态

**难点**：`IsBooked`（预约）与 `IsOccupied`（实占）分离——分配阶段占坑防撞，到位后才实占。

---

## 5. 入库主路径（口述版）

```text
Create → Approve
  → BuildPallet
       · 解析收货位 / PackId
       · 可选 Allocator → 目标位 Booking
       · 收货位入账；写 Detail
       · 收货≠目标 且有容器 → ITransportOrderRequest → Bus Leg
       · 同址平库 → 不建运，Detail 可直接 Completed
  → 包 AcceptLeg → …设备/仿真…
  → SegmentFeedback → Bus Complete
  → WmsTransportCompletionHandler → 库存到目标、单完成
```

**平库 vs 立库**：同址不建运是 PDA 友好路径；异址走总线是立库/四向路径——同一套 `BuildPallet` 分支。

---

## 6. 出库主路径

```text
OutboundOrder（头可带 WcsGroupNo；行 WcsPri / From / To / Container）
  → Approve：NeedsTransport 则预留 AvailableQty + 建运 RefType=OutboundOrder
  → 包 AcceptRetrieval：同组 Pri 门闩滚动
  → 完成回调正式 Ship
  → 平库 ShipAsync 可直发；已挂运禁止手发
```

**Pri 门闩**：同组更小优先级未终态 → 当前 Suspended；段完成再滚——保证出库顺序（如按波次/行号）。

---

## 7. 盘点

```text
CreatePlan（BookQty=当前库存或0）
  → RecordCount（实盘+Diff；Draft→Executing）
  → ConfirmAdjust（全盘完：Diff>0 Receive / <0 Ship → Completed）
```

可配合仓级锁，避免盘点期间分配进该仓。

---

## 8. PDA（Seven.App）

- `PdaService` / `PdaController`，JWT，`/api/pda/*`
- 菜单：收货 / 上架 / 盘点
- **薄**：复用 WMS 服务，不在 App 内复制库存逻辑

---

## 9. IWcsPack 契约（总线视角）

```text
PackId / CanHandle / AcceptLeg / CancelLeg / QueryLeg / Health
```

**故意不放进接口的**：寻路、流量、Codec、SUDR 细节——这些是包内私有，总线保持「薄」。

`CanHandle` 示例（四向）：两端 `Fw.` 或已知交接；纯 `Stk.*` 拒接。

---

## 10. 面试易问：库存会不会和 WCS 双写？

标准答：

> 可扣减数量只认 `Wms_Stock`。包内只有任务/路网/策略。预约用货位 `IsBooked`，占用用容器与货位状态。运输完成由 WMS Handler 统一落账，避免包内「影子库存」。
