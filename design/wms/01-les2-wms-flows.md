# LES2 WMS 业务流程说明书

状态：调研完成（只读 LES2，不引程序集）  
日期：2026-08-29  
源码根：`D:\Junheinrich\Project\FJD\SrcCode\LES2`  

> 磁盘目录为 `LES.Entities\Models\WMS`、`Models\BasicData`（无中文后缀）。

---

## 1. 主数据（BasicData）

### 1.1 组织层级

```text
Warehouse
  └── Zone（库区：Asrs / Floor / Dense / MiniLoad…）
        └── Aisle（巷道：可入可出、WcsAisleNo）
              └── Location（货位：Row/Col/Layer/Deepth；类型 Bin/Receiving/Outbound/Srm/Plc…）
```

| 实体 | 路径 | 要点 |
|------|------|------|
| Warehouse | `Models\BasicData\Warehouse.cs` | ERP/本地仓、FIFO/FEFO、盘点锁 |
| Zone | `Zone.cs` | 分配策略、混放、拣货路径开关 |
| Aisle | `Aisle.cs` | 启停、只入只出、空位权重、EP 点 |
| Location | `Location.cs` | 坐标、占用/预约、深浅锁、高重、ABC |
| Container / ContainerType | 容器 | 库存位置**通过容器**挂到 Location |
| Stock | 库存 | 无 LocationCode；靠 `ContainerCode` 间接定位 |
| SubMaterials | SKU | 货主、保质期、默认仓区、分配策略 |

### 1.2 库存模型特点

- **收货组盘即入账**（`StockStatus≈Receive`），上架完成主要改容器/货位与单据态，不是二次加库存。
- 预约出库增加 `BookQuantity`，拣选确认再扣减。

---

## 2. 业务单据形态

LES2 为「多单据类型 × 同构三层」：

```text
*Order（头）→ *OrderItem（行）→ *OrderDetail（组盘/执行明细）
```

入库类型：采购 / 调拨 / 生产 / 销退 / 其他 / 空托回收…  
出库类型：销售 / 调拨 / 其他 / 采退 / 领料 / 质检… + **Waybill（运单）** + **PickingTask（拣选）**。

Seven 已收敛为 **Inbound / Outbound / CycleCount** + `OrderType`，不 1:1 搬多套表。

---

## 3. 入库流程（立库）

以采购入库为代表（其他入库服务同构）。

```text
ERP/手工建单 (LesOrderStatus.Create, ReceiveStatus.Pending)
  → Receiving 组盘（同一事务）
       · 建 Container
       · 建 Stock（已入账）
       · 写 OrderDetail
       · 更新收货数量/收货状态
       · ★ 创建 StorageTask (Status=Create, SourceOrderId=Detail.Id)
  → 设备到申请点 DestinationRequest
       · AisleRequest → StorageTask.AisleAssigned + DesAddress≈巷道口
       · LocationRequest → LocationAssigned + DesAddress=货位
  → TransportTask → 设备执行
  → FinishStorageTask
       · 容器落到 DesAddress；立库 UnBookAndOccupy
       · CompleteDetailFromStorageTask → 单据推进/回传 ERP
```

### 3.1 StorageTask（WMS↔WCS 入库交接点）

| 字段语义 | 说明 |
|----------|------|
| SourceFrom | 入库来源类型（采购/生产/…） |
| SourceOrderId | **入库 Detail.Id** |
| Type | Asrs / Floor / Stable… |
| ContainerCode / Height / Weight | 执行约束 |
| SrcAddress / DesAddress | 申请点写入目的地 |
| Status | Create(10)→AisleAssigned(20)→LocationAssigned(30)→Finish(40) |

实体路径：`Models\WMS\Inbound\StorageTask.cs`（`EntityCategory=WCS`）  
服务：`LES.BLL\Service\ExtendService\WCS\StorageTaskService.cs`  
申请栈：`ExecuteStack\WCS_Stack\InboundExecuteStack.cs`

### 3.2 平库变体

`Receiving4Floor` / `Putaway4Floor`：可合并地堆容器；PDA 主导上架确认（见 Seven.App 方案）。

---

## 4. 出库流程（立库）

```text
建出库单 Create
  → StockPreAssign（干跑/预分配态）
  → StockAssign（BookQuantity + 生成 PickingTask）
  → ExecuteOutbound（须绑月台 Berth；可设 WcsGroupNo）
       · ★ CreateRetrievalTask(pickingTasks, WcsGroupNo, WcsPri)
       · Header/Item → OutboundAndPicking
       · ExecuteRetrievalTask → RetrievalExecuteTask + TransportTask
  → 设备完成 FinishRetrievalTask
       · AutoPickByContainer / 写实出 Detail
       · 同组按 WcsPri 滚动下发下一单
```

### 4.1 RetrievalTask（WMS↔WCS 出库交接点）

| 字段语义 | 说明 |
|----------|------|
| SourceOrderId | **PickingTask.Id**（不是出库单头） |
| ContainerCode / DesAddress | 源托与目的（月台/出库位） |
| WcsGroupNo / WcsPri | 组批与组内优先级 |
| AisleId / Material / Batch | 执行与干涉判断 |
| Status / RetrievalTaskStatus | Create→Execute→Complete / Suspend… |

实体：`Models\WMS\Outbound\RetrievalTask.cs`、`RetrievalExecuteTask.cs`  
服务：`RetrievalTaskService.cs`（含 `WcsGroupDispatch`）

### 4.2 与入库对称点

| | 入库 | 出库 |
|--|------|------|
| WMS 锚点 | StorageTask | RetrievalTask (+ ExecuteTask) |
| 溯源 | 入库 Detail | 拣选任务 |
| 设备单 | TransportTask | TransportTask（可含 TransferBin 让位） |
| 完成回写 | FinishStorageTask | FinishRetrievalTask → 滚动 |

---

## 5. 盘点摘要

`CyclecountPlan` → 预约锁仓 → 立库可 `CreateRetrievalTask(Cyclecount)` 拉到工位 → 录入 → 差异 → 调账。  
组盘前会拦截未完成盘点。

---

## 6. 相对 Seven 的语义映射（概念）

| LES2 | Seven（目标） |
|------|----------------|
| 多套 *InboundOrder | `Wms_InboundOrder` + OrderType |
| 多套 *OutboundOrder + Waybill | `Wms_OutboundOrder` + OrderType；（运单二期） |
| PickingTask | 出库预约明细 / 拣选任务表（待补齐） |
| StorageTask | **入向执行意图**：Inbound 完成后建 `Bus_TransportOrder`，包内 `Stk_PutAwayTask` 等 |
| RetrievalTask | **出向执行意图**：对称包内 Retrieval/取货任务 + 总线 Leg |
| Stock via Container | 保留 Container；`Wms_Stock` 可同时存 LocationCode 便于查询（迁移期双写再收敛） |
| DestinationRequest 分配 | **各 WCS 包内**（见 srm-wcs）；WMS 不实现巷道轮转 |

---

## 7. 关键绝对路径速查

```
LES.Entities\Models\BasicData\
LES.Entities\Models\WMS\Inbound|Outbound\
LES.Entities\LES\Enums\Enums.WMS.cs | Enums.BasicInfo.cs | Enums.WCS.cs
LES.BLL\Service\ExtendService\WMS\PurchaseInboundOrder\
LES.BLL\Service\ExtendService\WMS\SalesOutboundOrder\
LES.BLL\Service\ExtendService\WCS\StorageTaskService.cs
LES.BLL\Service\ExtendService\WCS\RetrievalTaskService.cs
LES.BLL\Service\ExecuteStack\WCS_Stack\InboundExecuteStack.cs
LES.BLL\Service\ExecuteStack\WMS_Stack\Stock\StockExecuteStack.cs
```
