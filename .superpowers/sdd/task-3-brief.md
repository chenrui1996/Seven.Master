### Task 3: WMS 主数据与账本（Stock / Location / Container）

**Files:**
- Create: `Seven.Net8/Seven.Domain/Entities/Wms/` — entities below
- Create: `Seven.Net8/Seven.Infrastructure/Persistence/Configurations/Wms/*.cs` — all `ToTable(TablePrefixes.Wms + "Name")`
- Create: `Seven.Net8/Seven.Application/Wms/IStockService.cs`, `ILocationService.cs`, `IContainerService.cs`
- Create: `Seven.Net8/Seven.Infrastructure/Wms/*Service.cs`
- Modify: `Seven.Net8/Seven.Infrastructure/Persistence/SevenDbContext.cs` — DbSets
- Modify: `Seven.Net8/Seven.Infrastructure/Wcs/WcsServiceCollectionExtensions.cs` OR new `WmsServiceCollectionExtensions.cs` — register WMS services when `Features.Wms`；推荐独立 `AddSevenWms` 并在 `AddSevenInfrastructure` 中调用
- Migration: `dotnet ef migrations add AddWmsMasterAndStock --project Seven.Infrastructure --startup-project Seven.WebApi` → 输出须在 `Seven.Infrastructure/Migrations/`
- Optional API: 基础查询 Controllers under `WebApi/Controllers/Wms/` with feature check on `Features.Wms`
- Test: `Seven.Net8/Seven.Tests/Wms/StockServiceTests.cs`

**实体最小集与表名：**

| 实体类 | 表名 |
|--------|------|
| `WmsWarehouse` | `Wms_Warehouse` |
| `WmsZone` | `Wms_Zone` |
| `WmsLocation` | `Wms_Location` |
| `WmsContainer` | `Wms_Container` |
| `WmsContainerType` | `Wms_ContainerType` |
| `WmsStock` | `Wms_Stock` |
| `WmsStockLedger` | `Wms_StockLedger` |

**字段指导（精简，继承 `BaseEntity`）：**
- Warehouse: Id (int PK), Code, Name
- Zone: Id, WarehouseId, Code, Name
- Location: Id, WarehouseId, ZoneId?, Code (unique), Aisle?, Row?, Column?, Layer?, IsOccupied, IsLocked, IsHandover, CurrentContainerCode?
- ContainerType: Id, Code, Name
- Container: Id, Code (unique), ContainerTypeId?, LocationCode?, Status
- Stock: Id, LocationCode, ContainerCode?, MaterialCode, Qty, AvailableQty, Lot?
- StockLedger: Id, StockId?, MaterialCode, LocationCode, ContainerCode?, DeltaQty, Reason, RefType?, RefId?

**服务最小 API：**
- `IStockService.ReceiveAsync` — 在指定库位增加库存并写流水；可同时占用库位/绑定容器
- `IStockService.ShipAsync` — 扣减 Available/Qty，写流水；数量不足抛领域异常
- 可选 `MoveAsync` 或仅用 Receive+Ship 测试守恒

**测试（EF InMemory 或 SQLite in-memory 均可）：**
1. Receive 后 Qty/Available 正确，Location.IsOccupied 合理
2. Ship 不足失败
3. Receive 再 Ship 后数量守恒（或库存为 0）

**硬规则：**
- 只用 `TablePrefixes.Wms`
- 不要实现入库/出库单据（Task 4）
- 不要实现总线（Task 5）
- 不要 commit
- 不要引用 LES2 程序集；字段不要照搬预留客户/销售组织等

**DI：** `Features.Wms==false` 时可仍注册实现（简单）或注册 NoOp；推荐始终注册服务，API 用 feature 门控即可。
