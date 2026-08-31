# Task 3 Report: WMS 主数据与账本

**Branch:** `feature/wms-wcs-pack`  
**Date:** 2026-08-29  
**Status:** DONE  
**Commits:** none (per user rule)

---

## Summary

Implemented WMS master data and location-level stock ledger (no inbound/outbound documents, no orchestration bus):

- 7 entities under `Seven.Domain/Entities/Wms/` (`BaseEntity` + explicit `Id` PK)
- Fluent configs `ToTable(TablePrefixes.Wms + "…")` → `Wms_Warehouse` / `Zone` / `Location` / `Container` / `ContainerType` / `Stock` / `StockLedger`
- `IStockService.ReceiveAsync` / `ShipAsync` with ledger + location occupancy
- `ILocationService` / `IContainerService` query + thin CRUD
- `AddSevenWms` always registers services; API gated by `[RequiresFeature("Wms")]`
- EF migration `AddWmsMasterAndStock` in `Seven.Infrastructure/Migrations/`
- Optional query controllers under `WebApi/Controllers/Wms/`

---

## TDD Evidence

### RED — Step 1

**Action:** Created `Seven.Tests/Wms/StockServiceTests.cs` (3 tests) before any WMS production types.

**Command:**
```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~StockServiceTests" --no-restore
```

**Outcome:** Exit code **1** — compile errors (expected):
- `Seven.Application.Wms` / `Seven.Domain.Entities.Wms` / `Seven.Domain.Wms` / `Seven.Infrastructure.Wms` namespaces missing
- Type `IStockService` not found

### GREEN — Step 2–4

**Action:** Entities, Fluent configs, DbSets, services, DI, optional controllers, then tests.

**Command:**
```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~StockServiceTests"
```

**Outcome:** Exit code **0**
```
已通过! - 失败: 0，通过: 3，已跳过: 0，总计: 3
```

**Build + regression (Task 1–2):**
```powershell
dotnet build Seven.Net8/Seven.sln
dotnet test … --filter "FullyQualifiedName~StockServiceTests|FullyQualifiedName~FeatureOptionsWcsTests|FullyQualifiedName~EquipmentTriggerPortTests"
```
- Solution build: **0 errors** (3 pre-existing warnings unrelated to WMS)
- Tests: **7 passed** (3 Stock + 2 Feature flags + 2 TriggerPort)

---

## Files Changed

| Action | Path |
|--------|------|
| Create | `Seven.Net8/Seven.Domain/Entities/Wms/WmsWarehouse.cs` |
| Create | `Seven.Net8/Seven.Domain/Entities/Wms/WmsZone.cs` |
| Create | `Seven.Net8/Seven.Domain/Entities/Wms/WmsLocation.cs` |
| Create | `Seven.Net8/Seven.Domain/Entities/Wms/WmsContainerType.cs` |
| Create | `Seven.Net8/Seven.Domain/Entities/Wms/WmsContainer.cs` |
| Create | `Seven.Net8/Seven.Domain/Entities/Wms/WmsStock.cs` |
| Create | `Seven.Net8/Seven.Domain/Entities/Wms/WmsStockLedger.cs` |
| Create | `Seven.Net8/Seven.Domain/Wms/WmsDomainException.cs` |
| Create | `Seven.Net8/Seven.Domain/Enums/WmsEnums.cs` |
| Create | `Seven.Net8/Seven.Application/Wms/IStockService.cs` |
| Create | `Seven.Net8/Seven.Application/Wms/ILocationService.cs` |
| Create | `Seven.Net8/Seven.Application/Wms/IContainerService.cs` |
| Create | `Seven.Net8/Seven.Application/Wms/WmsModels.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Persistence/Configurations/Wms/WmsConfigurations.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Wms/StockService.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Wms/LocationService.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Wms/ContainerService.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Wms/WmsServiceCollectionExtensions.cs` |
| Modify | `Seven.Net8/Seven.Infrastructure/Persistence/SevenDbContext.cs` |
| Modify | `Seven.Net8/Seven.Infrastructure/DependencyInjection.cs` |
| Create | `Seven.Net8/Seven.WebApi/Controllers/Wms/WmsControllers.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Migrations/20260829042603_AddWmsMasterAndStock.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Migrations/20260829042603_AddWmsMasterAndStock.Designer.cs` |
| Modify | `Seven.Net8/Seven.Infrastructure/Migrations/SevenDbContextModelSnapshot.cs` |
| Create | `Seven.Net8/Seven.Tests/Wms/StockServiceTests.cs` |

---

## Implementation Notes

### Entities

All inherit `BaseEntity` with explicit `int Id` PK (DeviceComm style, field set from brief). Location coordinates, occupy/lock/handover, container binding retained. No customer/sales-org reserved fields.

### Stock commands

- `ReceiveAsync`: require location (not locked) → upsert stock by Location+Material+Container+Lot → Qty/Available += qty → occupy location → optional bind existing container → write ledger (`Reason` default `"Receive"`)
- `ShipAsync`: find stock; missing or `AvailableQty < qty` → `WmsDomainException("库存不足")`; else decrement, ledger (`"Ship"`), release location if no remaining qty at that code
- Quantity must be `> 0`
- Commands write DB only; no `ICacheService` (channel B later)

### DI

`AddSevenWms` always registers `IStockService` / `ILocationService` / `IContainerService` as scoped. Called from `AddSevenInfrastructure` after `AddSevenWcs`. Feature gate is on API (`[RequiresFeature("Wms")]`), not DI.

### Migration

```
dotnet ef migrations add AddWmsMasterAndStock --project Seven.Net8/Seven.Infrastructure --startup-project Seven.Net8/Seven.WebApi --output-dir Migrations --context SevenDbContext
```

Output is `Seven.Infrastructure/Migrations/` (namespace `Seven.Infrastructure.Migrations`). Seven tables only; DeviceComm tables not recreated.

### Tests (EF InMemory, package already present)

1. Receive 10 → Qty/Available = 10, `Location.IsOccupied`, one ledger +10  
2. Ship with no stock → `WmsDomainException`  
3. Receive 10 then Ship 10 → Qty/Available = 0, two ledgers sum to 0  

---

## Self-Review

| Check | Result |
|-------|--------|
| Table names only `TablePrefixes.Wms` | OK |
| No inbound/outbound/cycle-count documents | OK |
| No orchestration bus | OK |
| No LES2 assembly refs | OK |
| Migration in `Infrastructure/Migrations/` not `Persistence/Migrations` | OK |
| TDD RED→GREEN documented | OK |
| StockServiceTests 3/3 + solution builds | OK |
| Feature-gated API | OK |
| Git commit | none |

**Minor notes (non-blocking):**
- Receive persists stock then ledger in two `SaveChanges` so new `StockId` is available; InMemory has no transactions
- `WmsLocation.Column` is a MySQL reserved word; Pomelo quotes it
- Composite stock index allows multiple NULL `ContainerCode`/`Lot` rows in MySQL unique semantics
- Permission codes (`WmsLocation.Search` etc.) have no menu seed yet (Task 4)
- `MoveAsync` not added; conservation covered by Receive+Ship

---

## Review Fix: Atomic Receive (2026-08-29)

**Finding:** `ReceiveAsync` called `SaveChangesAsync` twice (stock/location, then ledger), risking partial commit.

**Fix:** Single unit-of-work save via EF navigation:
- Added `WmsStock? Stock` navigation on `WmsStockLedger`
- `AddLedger` sets `Stock = stock` instead of `StockId = stock.Id` (EF resolves FK on insert for new stock with `Id == 0`)
- `ReceiveAsync`: mutate stock/location → `AddLedger` → one `SaveChangesAsync`
- Updated `WmsStockLedgerConfiguration` to `HasOne(x => x.Stock)`

**Files changed:**
| Action | Path |
|--------|------|
| Modify | `Seven.Net8/Seven.Domain/Entities/Wms/WmsStockLedger.cs` |
| Modify | `Seven.Net8/Seven.Infrastructure/Persistence/Configurations/Wms/WmsConfigurations.cs` |
| Modify | `Seven.Net8/Seven.Infrastructure/Wms/StockService.cs` |

**Test re-run:**
```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~StockServiceTests"
```

**Outcome:** Exit code **0**
```
已通过! - 失败: 0，通过: 3，已跳过: 0，总计: 3
```

**Status:** FIXED — stock + location + ledger commit in one `SaveChangesAsync`; no DB migration needed (navigation-only).

---

## Concerns / Follow-ups (Task 4+)

1. **Atomic receive** — wrap stock+ledger in a relational transaction when not on InMemory
2. **Concurrency** — no row version; concurrent Ship can oversell
3. **Container auto-create** — Receive binds only if `Wms_Container` already exists; does not create container master
4. **API vs Features.Wms=false** — controllers return 404 until the flag is enabled
5. **WmsDomainException** — stock API maps to `WebResponseContent.Error`; other unhandled domain throws still 500 via middleware

---

## Out of Scope (confirmed not done)

- Inbound / outbound / cycle-count orders (Task 4)
- Orchestration bus / TransportOrder (Task 5)
- Entity cache channel B
- Git commit
