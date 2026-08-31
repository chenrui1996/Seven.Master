# Task 6 Report: 堆垛机 WCS 包（语义 SUDR/SUDS）

**Branch:** `feature/wms-wcs-pack`  
**Date:** 2026-08-29  
**Status:** DONE  
**Commits:** none (per user rule)

---

## Summary

Implemented the Stacker WCS pack with semantic destination request/dispatch (SUDR/SUDS) and no physical comms.

- `StackerWcsPack` (`PackId = "stacker"`): `AcceptLeg` creates `Stk_PutAwayTask` (Accepted); `CanHandle` always true when the pack is registered
- `StackerDestinationService` subscribes to `IEquipmentTriggerPort.DestinationRequested` / `SegmentFeedback`
  - AisleRequest → `SelectAisle` (height/weight/availability + rotate `Stk_AssignmentRecord`) → `DispatchDestination` to aisle EP
  - LocationRequest → `SelectLocation` in aisle via `Wms_Location` → dispatch to bin
  - Segment feedback → complete `Stk_DeviceTask` / putaway → `IOrchestrationBus.OnLegEvent(Completed)`
- Allocators isolated to `Stk_` policy tables; location coords read from `Wms_Location` by Code/Aisle only
- `StackerSchedulerHostedService` bridges Singleton port events to Scoped destination service
- DI when `Features.WcsPacks.Stacker`: pack, allocators, destination service, hosted service
- EF migration `AddStackerPack` (`Stk_RequestPoint`, `Stk_AssignmentPolicy`, `Stk_AssignmentRecord`, `Stk_PutAwayTask`, `Stk_DeviceTask`)

**Did not** touch FourWay / `Fw_` tables, PLC, or commit.

---

## TDD Evidence

### RED — Step 1

**Action:** Created `Seven.Tests/Wcs/StackerPackTests.cs` (3 tests) before production types.

**Command:**
```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~StackerPackTests"
```

**Outcome:** Exit code **1** — compile errors (expected):
- `Seven.Domain.Entities.Wcs` / `Seven.Infrastructure.Wcs.Packs` not found

### GREEN — Step 2

**Action:** Enums, `Stk_*` entities, Fluent configs, DbSets, allocators, pack, destination service, hosted service, DI, migration.

**Command:**
```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~StackerPackTests"
```

**Outcome:** Exit code **0**
```
已通过! - 失败: 0，通过: 3，已跳过: 0，总计: 3
```

**Regression:**
```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~OrchestrationBusTests"
```
```
已通过! - 失败: 0，通过: 3，已跳过: 0，总计: 3
```

---

## Files Changed

| Action | Path |
|--------|------|
| Create | `Seven.Net8/Seven.Domain/Enums/StackerEnums.cs` |
| Create | `Seven.Net8/Seven.Domain/Entities/Wcs/Stacker/StkRequestPoint.cs` |
| Create | `Seven.Net8/Seven.Domain/Entities/Wcs/Stacker/StkAssignmentPolicy.cs` |
| Create | `Seven.Net8/Seven.Domain/Entities/Wcs/Stacker/StkAssignmentRecord.cs` |
| Create | `Seven.Net8/Seven.Domain/Entities/Wcs/Stacker/StkPutAwayTask.cs` |
| Create | `Seven.Net8/Seven.Domain/Entities/Wcs/Stacker/StkDeviceTask.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Persistence/Configurations/Wcs/Stacker/StackerConfigurations.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Wcs/Packs/Stacker/StackerWcsPack.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Wcs/Packs/Stacker/StackerAisleAllocator.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Wcs/Packs/Stacker/StackerLocationAllocator.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Wcs/Packs/Stacker/StackerDestinationService.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Wcs/Packs/Stacker/StackerSchedulerHostedService.cs` |
| Modify | `Seven.Net8/Seven.Infrastructure/Persistence/SevenDbContext.cs` |
| Modify | `Seven.Net8/Seven.Infrastructure/Wcs/WcsServiceCollectionExtensions.cs` |
| Modify | `Seven.Net8/Seven.Infrastructure/Wcs/Bus/WcsPackResolver.cs` (comment only) |
| Create | `Seven.Net8/Seven.Infrastructure/Migrations/20260829044829_AddStackerPack.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Migrations/20260829044829_AddStackerPack.Designer.cs` |
| Modify | `Seven.Net8/Seven.Infrastructure/Migrations/SevenDbContextModelSnapshot.cs` |
| Create | `Seven.Net8/Seven.Tests/Wcs/StackerPackTests.cs` |

---

## Implementation Notes

### SelectAisle

Eligible policies: `IsAvailable` and request height/weight ≤ `MaxHeight`/`MaxWeight`. Rotate by oldest `Stk_AssignmentRecord.LastAssignedAt` (unassigned first), then AisleCode. Writes/updates the record.

### SUDR → SUDS

`DestinationRequested` looks up `Stk_RequestPoint` by `SourcePointCode`. AisleRequest assigns aisle + dispatches to `AssignmentPolicy.DestinationPointCode`. LocationRequest uses assigned aisle (or point.AisleCode) and `Wms_Location` (!occupied, !locked). Creates `Stk_DeviceTask` (Dispatched).

### Complete path

`SimulateSegmentFeedback` completes the open DeviceTask + PutAwayTask, then `OnLegEvent(Completed)`. E2E test uses real `OrchestrationBus` + `InMemoryEquipmentTriggerPort` + `StackerWcsPack`.

### DI

`AddSevenWcs` registers pack/allocators/destination/hosted service only when `Features.WcsPacks.Stacker`. `IOrchestrationBus` is optional (`GetService`) so Stacker can start without the bus feature.

### Migration

```
dotnet ef migrations add AddStackerPack --project Seven.Net8/Seven.Infrastructure --startup-project Seven.Net8/Seven.WebApi --output-dir Migrations --context SevenDbContext
```

Guid PKs on tasks use `ValueGeneratedNever`.

---

## Self-Review

| Check | Result |
|-------|--------|
| TDD RED→GREEN documented | OK |
| SelectAisle rotates / skips unavailable | OK |
| SimulateDestinationRequest → DispatchDestination | OK |
| AcceptLeg + feedback → OnLegEvent Completed | OK |
| Table prefixes `Stk_` only | OK |
| No FourWay / `Fw_` types | OK |
| Allocators do not share FourWay tables | OK |
| IEquipmentTriggerPort only (InMemory) | OK |
| Feature-gated DI | OK |
| Git commit | none |

---

## Concerns / Follow-ups

1. **CanHandle is always true** — V1 assumes a registered Stacker pack covers any from/to; multi-pack planning still needs handover links (Task 5).
2. **LocationRequest path is implemented but not covered by a dedicated test** — only AisleRequest is asserted in StackerPackTests.
3. **Inbound From=To** (Task 5) is unchanged; Stacker `CanHandle=true` will now accept same-location legs if the bus is on and Stacker is the only pack.
4. **No WMS e2e** (inbound → bus → stacker → stock at dest) — plan Step 5 deferred; completion handler remains NoOp.
5. **Hosted service event handlers create a new scope per trigger** — tests call `Subscribe()` on a constructed service instead of the hosted service.
6. **CheckResult must be `OK`** (case-insensitive) or SUDR is ignored.

---

## Out of Scope (confirmed not done)

- FourWay / `Fw_` / RGV
- Real PLC / Socket / Modbus / S7
- Git commit
- WMS inbound → stock-at-bin e2e
