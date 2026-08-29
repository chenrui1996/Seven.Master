### Task 4: WMS 三单收敛（入库 / 出库 / 盘点）

**目标：** 各仅一套单据表；`OrderType` 区分来源。参考 LES2 状态机思路，精简字段。

**表（全 `Wms_` 前缀）：**
- `Wms_InboundOrder` / `Wms_InboundOrderLine`
- `Wms_OutboundOrder` / `Wms_OutboundOrderLine`
- `Wms_CycleCount` / `Wms_CycleCountLine` / `Wms_CycleCountRecord`（可合并 Line+Record 若更简单：计划行 + 实盘数量字段）

**实体要点：**
- Header: Id, OrderNo (unique), OrderType (string/enum: Purchase/Production/Other/…), Status (Draft/Approved/Executing/Completed/Cancelled)
- Line: Id, OrderId, LineNo, MaterialCode, Qty, CompletedQty?, ContainerCode?, FromLocation?/ToLocation?
- CycleCount: Plan + lines with BookQty/CountQty/DiffQty

**服务：**
- `IInboundOrderService`: Create, Approve, ReceiveAndBuildPallet (调用已有 `IStockService.ReceiveAsync` 到收货位；**不要**调用总线——总线在 Task 5；可留 `ITransportOrderRequest` 钩子接口空实现或 TODO 注释接口注入 optional)
- `IOutboundOrderService`: Create, Approve, AllocateAndReserve (扣 Available 或单独预留字段；精简：Approve 后 Ship 从指定库位)
- `ICycleCountService`: CreatePlan, RecordCount, ConfirmAdjust (调账用 Stock Receive/Ship 差额)

**测试：**
1. 入库：Create→Approve→收货组盘→Stock 增加
2. 出库：有库存→Approve→Ship→库存减少
3. 盘点：账面与实盘差异→ConfirmAdjust→库存变为实盘数

**不要：** TransportOrder、WCS 包、commit、多套单据表、Waybill。

**迁移：** `AddWmsOrders` 到 `Infrastructure/Migrations/`。

**DI：** 在 `AddSevenWms` 注册新服务。

**可选：** 薄 API Controllers + `[RequiresFeature("Wms")]`。
