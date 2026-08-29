# Task 4 Report: WMS 三单（入库 / 出库 / 盘点）

**Branch:** `feature/wms-wcs-pack`  
**Date:** 2026-08-29  
**Status:** DONE  
**Commits:** none (per user rule)

---

## Summary

Implemented one inbound, one outbound, and one cycle-count document set (`OrderType` distinguishes source). Reused `IStockService.ReceiveAsync` / `ShipAsync`. Did **not** call the orchestration bus.

- Entities: `WmsInboundOrder`/`Line`, `WmsOutboundOrder`/`Line`, `WmsCycleCount`/`Line` (Line+Record merged: `BookQty`/`CountQty`/`DiffQty`/`Counted`)
- Status: `WmsOrderStatus` Draft → Approved → Executing → Completed | Cancelled
- Services: `IInboundOrderService` (Create/Approve/ReceiveAndBuildPallet), `IOutboundOrderService` (Create/Approve/AllocateAndReserve/Ship), `ICycleCountService` (CreatePlan/RecordCount/ConfirmAdjust)
- `ITransportOrderRequest` + `NoOpTransportOrderRequest` registered; inbound **does not invoke** it (TODO Task 5)
- `AddSevenWms` registers the three order services
- EF migration `AddWmsOrders` → 6 tables `Wms_*`
- Optional thin controllers `[RequiresFeature("Wms")]`

---

## TDD Evidence

### RED — Step 1

**Action:** Created `Seven.Tests/Wms/WmsOrderServiceTests.cs` (3 tests) before order production types.

**Command:**
```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~WmsOrderServiceTests" --no-restore
```

**Outcome:** Exit code **1** — compile errors (expected):
- `IInboundOrderService` / `IOutboundOrderService` / `ICycleCountService` not found

### GREEN — Step 2

**Action:** Enums, entities, Fluent configs, DbSets, services, DI, controllers, migration.

**Command:**
```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~WmsOrderServiceTests|FullyQualifiedName~StockServiceTests"
```

**Outcome:** Exit code **0**
```
已通过! - 失败: 0，通过: 6，已跳过: 0，总计: 6
```
- `WmsOrderServiceTests`: 3/3
- `StockServiceTests`: 3/3 (no regression)

---

## Files Changed

| Action | Path |
|--------|------|
| Create | `Seven.Net8/Seven.Domain/Entities/Wms/WmsInboundOrder.cs` |
| Create | `Seven.Net8/Seven.Domain/Entities/Wms/WmsInboundOrderLine.cs` |
| Create | `Seven.Net8/Seven.Domain/Entities/Wms/WmsOutboundOrder.cs` |
| Create | `Seven.Net8/Seven.Domain/Entities/Wms/WmsOutboundOrderLine.cs` |
| Create | `Seven.Net8/Seven.Domain/Entities/Wms/WmsCycleCount.cs` |
| Create | `Seven.Net8/Seven.Domain/Entities/Wms/WmsCycleCountLine.cs` |
| Modify | `Seven.Net8/Seven.Domain/Enums/WmsEnums.cs` |
| Create | `Seven.Net8/Seven.Application/Wms/IInboundOrderService.cs` |
| Create | `Seven.Net8/Seven.Application/Wms/IOutboundOrderService.cs` |
| Create | `Seven.Net8/Seven.Application/Wms/ICycleCountService.cs` |
| Create | `Seven.Net8/Seven.Application/Wms/ITransportOrderRequest.cs` |
| Modify | `Seven.Net8/Seven.Application/Wms/WmsModels.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Wms/InboundOrderService.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Wms/OutboundOrderService.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Wms/CycleCountService.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Wms/WmsTransaction.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Wms/NoOpTransportOrderRequest.cs` |
| Modify | `Seven.Net8/Seven.Infrastructure/Wms/WmsServiceCollectionExtensions.cs` |
| Modify | `Seven.Net8/Seven.Infrastructure/Persistence/Configurations/Wms/WmsConfigurations.cs` |
| Modify | `Seven.Net8/Seven.Infrastructure/Persistence/SevenDbContext.cs` |
| Modify | `Seven.Net8/Seven.WebApi/Controllers/Wms/WmsControllers.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Migrations/20260829043537_AddWmsOrders.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Migrations/20260829043537_AddWmsOrders.Designer.cs` |
| Modify | `Seven.Net8/Seven.Infrastructure/Migrations/SevenDbContextModelSnapshot.cs` |
| Create | `Seven.Net8/Seven.Tests/Wms/WmsOrderServiceTests.cs` |

---

## Implementation Notes

### Inbound

Create (Draft) → Approve → `ReceiveAndBuildPallet`: create container if missing, `IStockService.ReceiveAsync` to line `ToLocation`, `CompletedQty`, complete when all lines done.

### Outbound

Create → Approve → optional `AllocateAndReserve` (deduct `AvailableQty`) → `Ship` via `IStockService.ShipAsync`. If reserved, temporarily restore `AvailableQty` so `ShipAsync` can decrement both Qty and Available.

### Cycle count

`CreatePlan` snapshots `BookQty` from stock. `RecordCount` sets `CountQty`/`DiffQty`/`Counted`. `ConfirmAdjust` Receive (surplus) or Ship (shortage) the diff; requires every line counted.

### Transactions

Multi-step commands (`ReceiveAndBuildPallet`, `Ship`, `ConfirmAdjust`) wrap StockService saves + order update in `WmsTransaction`. Relational providers: `BeginTransaction`. InMemory: skip (provider throws `TransactionIgnoredWarning`).

### Migration

```
dotnet ef migrations add AddWmsOrders --project Seven.Net8/Seven.Infrastructure --startup-project Seven.Net8/Seven.WebApi --output-dir Migrations --context SevenDbContext
```

Tables: `Wms_InboundOrder`, `Wms_InboundOrderLine`, `Wms_OutboundOrder`, `Wms_OutboundOrderLine`, `Wms_CycleCount`, `Wms_CycleCountLine`. Unique `OrderNo` per header; unique `(OrderId, LineNo)` per line.

---

## Self-Review

| Check | Result |
|-------|--------|
| One inbound / one outbound / one cycle-count set | OK |
| `OrderType` on inbound/outbound | OK |
| Reuse `IStockService`; no bus call | OK |
| Table names `TablePrefixes.Wms` | OK |
| Multi-step in transaction (relational) | OK |
| Migration in `Infrastructure/Migrations/` | OK |
| TDD RED→GREEN documented | OK |
| StockServiceTests still 3/3 | OK |
| Git commit | none |

**Self-review fix:** `ConfirmAdjust` originally treated unrecorded lines as CountQty=0. Added `Counted`; refuse if any line not recorded.

---

## Concerns / Follow-ups (Task 5+)

1. **Nested SaveChanges** — StockService still saves internally; atomicity relies on ambient relational transaction, not a single `SaveChanges`.
2. **AllocateAndReserve** — unreserve-then-Ship is fragile; a `ShipReserved` on StockService would be cleaner.
3. **No `Wms_CycleCountRecord`** — merged into line (brief-allowed).
4. **No menu seed / Vue list pages** — plan mentioned; brief did not require.
5. **ITransportOrderRequest** injected but unused until Task 5 wires `CreateTransportOrder`.
6. **Concurrency** — still no row version on stock or orders.

---

## Out of Scope (confirmed not done)

- Orchestration bus / TransportOrder (Task 5)
- Waybill, extra document tables
- Git commit
