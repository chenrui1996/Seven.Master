# FourWay WCS Sprint 1 — Task 1 Report

**Task:** Domain + EF (Policy / PutAway / RequestPoint)  
**Date:** 2026-08-29  
**Status:** DONE

---

## Summary

Implemented Sprint 1 Task 1 per `fourway-task-1-brief.md`: FourWay domain enums, four new entities, `FwLayerPolicy.AllocationWeight`, EF configurations, DbSets, and migration `AddFwPolicyPutAwayRequest`. No Allocator, DestinationService, or WcsPack changes (deferred to Tasks 2–3).

---

## Deliverables Checklist

| Item | Status |
|------|--------|
| `FwPutAwayStatus` enum | Done |
| `FwRequestPointType` enum | Done |
| `FwAislePolicy` entity | Done |
| `FwAssignmentRecord` entity | Done |
| `FwPutAwayTask` entity | Done |
| `FwRequestPoint` entity | Done |
| `FwLayerPolicy.AllocationWeight` (default 1) | Done |
| EF configs in `FourWayConfigurations.cs` | Done |
| DbSets in `SevenDbContext.cs` | Done |
| Migration `AddFwPolicyPutAwayRequest` | Done |
| `dotnet build` pass | Done |
| Git commit | Skipped (by policy) |

---

## Files Created

| Path | Description |
|------|-------------|
| `Seven.Domain/Entities/Wcs/FourWay/FwAislePolicy.cs` | 巷分配策略 |
| `Seven.Domain/Entities/Wcs/FourWay/FwAssignmentRecord.cs` | 层/巷轮转记录 |
| `Seven.Domain/Entities/Wcs/FourWay/FwPutAwayTask.cs` | 入库上架任务 |
| `Seven.Domain/Entities/Wcs/FourWay/FwRequestPoint.cs` | 目的地申请点 |
| `Seven.Infrastructure/Persistence/Migrations/20260829114832_AddFwPolicyPutAwayRequest.cs` | EF migration Up/Down |
| `Seven.Infrastructure/Persistence/Migrations/20260829114832_AddFwPolicyPutAwayRequest.Designer.cs` | Migration designer |

## Files Modified

| Path | Change |
|------|--------|
| `Seven.Domain/Enums/FourWayEnums.cs` | Added `FwPutAwayStatus`, `FwRequestPointType`, `FwAssignmentScopeType` |
| `Seven.Domain/Entities/Wcs/FourWay/FwLayerPolicy.cs` | Added `AllocationWeight` (default 1) |
| `Seven.Infrastructure/Persistence/Configurations/Wcs/FourWay/FourWayConfigurations.cs` | Added 4 entity configs; extended `FwLayerPolicyConfiguration` |
| `Seven.Infrastructure/Persistence/SevenDbContext.cs` | Added 4 DbSets |
| `Seven.Infrastructure/Migrations/SevenDbContextModelSnapshot.cs` | Auto-updated by EF |

---

## Entity Details

### Enums (`FourWayEnums.cs`)

```csharp
public enum FwPutAwayStatus
{
    Accepted = 0,
    LayerAssigned = 1,
    AisleAssigned = 2,
    LocationAssigned = 3,
    Completed = 4,
    Cancelled = 5,
    Failed = 6
}

public enum FwRequestPointType
{
    LayerRequest = 0,
    AisleRequest = 1,
    LocationRequest = 2
    // F5: Hoist* later
}
```

Additionally added `FwAssignmentScopeType { Layer = 0, Aisle = 1 }` for `FwAssignmentRecord.ScopeType` (brief: "ScopeType Layer|Aisle").

### `FwAislePolicy`

Fields: `LayerCode`, `AisleCode`, `MinEmptySlots`, `MaxShuttleCount`, `DestinationPointCode`, `AllocationWeight` (default 1), `IsAvailable`, `MaxHeight` (default 9999), `MaxWeight` (default 99999, decimal).

Unique index: `(LayerCode, AisleCode)`.

### `FwAssignmentRecord`

Fields: `ScopeType` (`FwAssignmentScopeType`), `ScopeCode`, `LastAssignedAt`, `AssignCount`.

Unique index: `(ScopeType, ScopeCode)` — generalizes Stacker's per-aisle record for layer and aisle rotation.

### `FwPutAwayTask`

Mirrors `StkPutAwayTask` with FourWay-specific assignment fields:

- `Id` (Guid), `LegId`, `ContainerCode`, `FromCode`, `ToCode`, `Status` (`FwPutAwayStatus`)
- `AssignedLayer`, `AssignedAisle`, `AssignedLocationCode` (nullable strings)

Indexes: unique `LegId`; `ContainerCode`; `Status`.

### `FwRequestPoint`

Fields: `Code`, `PointType`, `IsEnabled`, `LayerCode?`, `AisleCode?`.

Unique index on `Code`. Extends Stacker pattern with optional `LayerCode` for layer-level SUDR.

### `FwLayerPolicy` extension

Added `AllocationWeight` int default 1; EF default value 1 in config.

---

## Migration `AddFwPolicyPutAwayRequest`

**Up:**

1. `ALTER Fw_LayerPolicy ADD AllocationWeight int NOT NULL DEFAULT 1`
2. `CREATE Fw_AislePolicy` — full policy columns + BaseEntity audit
3. `CREATE Fw_AssignmentRecord` — ScopeType/ScopeCode/LastAssignedAt/AssignCount
4. `CREATE Fw_PutAwayTask` — Guid PK, Leg 1:1, assignment fields
5. `CREATE Fw_RequestPoint` — Code/PointType/LayerCode/AisleCode
6. Indexes as configured

**Down:** Drops four new tables; removes `AllocationWeight` from `Fw_LayerPolicy`.

---

## Pattern Alignment

| Aspect | Stacker reference | FourWay implementation |
|--------|-------------------|------------------------|
| Table prefix | `Stk_` | `Fw_` via `TablePrefixes.FourWay` |
| PutAway task | `StkPutAwayTask` | `FwPutAwayTask` + `AssignedLayer` |
| Request point | `StkRequestPoint` | `FwRequestPoint` + `LayerCode` |
| Assignment policy | `StkAssignmentPolicy` | `FwAislePolicy` (scoped by LayerCode) |
| Rotation record | `StkAssignmentRecord` (AisleCode only) | `FwAssignmentRecord` (ScopeType + ScopeCode) |
| Base entity | `BaseEntity` | Same (TenantId, soft delete, audit) |

**Not shared:** No reference to `Stk_AssignmentPolicy`; independent `Fw_*` tables per global constraints.

---

## Build / Test

```
dotnet build Seven.Net8/Seven.sln
→ 成功，0 错误，3 个预存警告（DeviceComm / WorkFlowService）
```

No new tests added (Task 1 scope). Tests for Allocator come in Task 2.

---

## Self-Review

### Correctness

- All brief-specified types and fields present.
- EF configs follow existing Stacker/FourWay conventions (max lengths, precision, indexes, defaults).
- `FourWayInboundAllocator` unchanged (F1 skeleton intact for Task 2).
- No RCS4Shuttle references; no DeviceComm/F6 work.

### Minor decisions (documented)

1. **`FwAssignmentScopeType` enum** — Brief says "ScopeType Layer|Aisle" without naming an enum; added explicit enum for type safety (matches EF int column pattern used elsewhere).
2. **`MaxWeight` as `decimal(18,4)`** — Matches `FwLayerPolicy`, not Stacker's `int MaxWeight`.
3. **`MaxHeight`/`MaxWeight` defaults on `FwAislePolicy`** — Set to 9999/99999 like `FwLayerPolicy` for permissive defaults when policies are seeded without explicit limits.

### Concerns

1. **Split migration folders** — New migration landed in `Persistence/Migrations/` (namespace `Seven.Infrastructure.Persistence.Migrations`) while `SevenDbContextModelSnapshot.cs` remains under `Infrastructure/Migrations/`. This matches recent Stacker migrations in the repo; EF CLI succeeded and snapshot updated. Operators should run `dotnet ef database update` as usual from README.
2. **`FwAssignmentScopeType` not in brief enum list** — Required for typed ScopeType; values Layer=0 / Aisle=1 are stable for Task 2 rotation logic.

---

## Out of Scope (confirmed not touched)

- `FourWayInboundAllocator.cs` rewrite (Task 2)
- `FourWayDestinationService` (Task 3)
- `FourWayWcsPack` AcceptLeg / CanHandle (Task 3)
- Unit / E2E tests
- Git commit

---

## Next Steps (Task 2)

1. Rewrite `FourWayInboundAllocator` to use `FwLayerPolicy`, `FwAislePolicy`, `FwAssignmentRecord`.
2. Add `FourWayAllocatorTests.cs` with weight / MinEmptySlots / rotation / booking cases.
3. Apply migration to dev DB if not auto-migrated on startup.
