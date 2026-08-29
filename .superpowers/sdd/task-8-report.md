# Task 8 Report: 四向车 WCS 包（寻路+最小交通）

**Branch:** `feature/wms-wcs-pack`  
**Date:** 2026-08-29  
**Status:** DONE  
**Commits:** none (per user rule)

---

## Summary

Implemented the FourWay WCS pack with weighted shortest-path routing and V1 head-on traffic, isolated from Stacker (`Stk_` / allocators).

- `FourWayWcsPack` (`PackId = "fourway"`): `AcceptLeg` creates `Fw_ShuttleTask` (Accepted); `CanHandle` is true when the pack is registered
- `FourWayRouter.FindPath` — Dijkstra on a directed weighted graph (pure function)
- `FourWayTrafficGuard` — occupy `fw:flow:{edgeId}`; second grant on the same edge (including opposite direction) fails
  - Uses `IHotStore` when it is not `DisabledHotStore`
  - Falls back to `ConcurrentDictionary` when HotStore feature is off (tests construct with no store)
- `FourWaySchedulerHostedService` — start/stop log only (no physical comm)
- DI when `Features.WcsPacks.FourWay`: pack, singleton traffic guard, hosted service
- EF migration `AddFourWayPack` (`Fw_MapVersion`, `Fw_Node`, `Fw_Route`, `Fw_RouteGroup`, `Fw_ShuttleTask`, `Fw_ShuttleTaskPath`)

**Did not** import Stacker allocators / `Stk_` entities, port `FlowConflictAnalyzer`, bind PLC, or commit.

---

## TDD Evidence

### RED — Step 1

**Action:** Created `Seven.Tests/Wcs/FourWayPackTests.cs` (3 tests) before production types.

**Command:**
```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~FourWayPackTests"
```

**Outcome:** Exit code **1** — compile errors (expected):
- `Seven.Domain.Entities.Wcs.FourWay` not found
- `Seven.Infrastructure.Wcs.Packs.FourWay` not found

### GREEN — Step 2

**Action:** Enums, `Fw_*` entities, Fluent configs, DbSets, router, traffic guard, pack, hosted service, DI, migration.

**Command:**
```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~FourWayPackTests"
```

**Outcome:** Exit code **0**
```
已通过! - 失败: 0，通过: 3，已跳过: 0，总计: 3
```

**Regression:**
```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~FourWayPackTests|FullyQualifiedName~StackerPackTests|FullyQualifiedName~OrchestrationBusTests"
```
```
已通过! - 失败: 0，通过: 9，已跳过: 0，总计: 9
```

| Test | Asserts |
|------|---------|
| `Router_ShouldFindPath_OnThreeNodeGraph` | A→C→B on 3-node weighted graph |
| `Traffic_ShouldReject_HeadOnOppositeDirection` | occupy A→B, B→A grant fails (in-memory fallback) |
| `AcceptLeg_ShouldCreateShuttleTask_WhenCanHandle` | PackId=`fourway`, CanHandle=true, `Fw_ShuttleTask` persisted |

---

## Files Changed

| Action | Path |
|--------|------|
| Create | `Seven.Net8/Seven.Domain/Enums/FourWayEnums.cs` |
| Create | `Seven.Net8/Seven.Domain/Entities/Wcs/FourWay/FwMapVersion.cs` |
| Create | `Seven.Net8/Seven.Domain/Entities/Wcs/FourWay/FwNode.cs` |
| Create | `Seven.Net8/Seven.Domain/Entities/Wcs/FourWay/FwRoute.cs` |
| Create | `Seven.Net8/Seven.Domain/Entities/Wcs/FourWay/FwRouteGroup.cs` |
| Create | `Seven.Net8/Seven.Domain/Entities/Wcs/FourWay/FwShuttleTask.cs` |
| Create | `Seven.Net8/Seven.Domain/Entities/Wcs/FourWay/FwShuttleTaskPath.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Persistence/Configurations/Wcs/FourWay/FourWayConfigurations.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Wcs/Packs/FourWay/FourWayRouter.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Wcs/Packs/FourWay/FourWayTrafficGuard.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Wcs/Packs/FourWay/FourWayWcsPack.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Wcs/Packs/FourWay/FourWaySchedulerHostedService.cs` |
| Modify | `Seven.Net8/Seven.Infrastructure/Persistence/SevenDbContext.cs` |
| Modify | `Seven.Net8/Seven.Infrastructure/Wcs/WcsServiceCollectionExtensions.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Migrations/20260829045539_AddFourWayPack.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Migrations/20260829045539_AddFourWayPack.Designer.cs` |
| Modify | `Seven.Net8/Seven.Infrastructure/Migrations/SevenDbContextModelSnapshot.cs` |
| Create | `Seven.Net8/Seven.Tests/Wcs/FourWayPackTests.cs` |

---

## Implementation Notes

### Router

Dijkstra on directed `FourWayEdge` list. Test graph: A—C—B (weight 1). Same from/to returns a single-node path; unreachable returns empty.

### Traffic

Key `fw:flow:{edgeId}`. V1 exclusive occupancy of the edge: a second owner (including opposite direction) is denied. Same owner re-grant succeeds. `DisabledHotStore` / null store → process-local dictionary so tests work with `Features.HotStore=false`.

### AcceptLeg

Writes `Fw_ShuttleTask` (idempotent on `LegId`). Honors `IControlModeService` EStop/Manual like Stacker. Does **not** yet persist `Fw_ShuttleTaskPath` or call the router (map seed not required for V1 accept).

### DI

`AddSevenWcs` registers FourWay only when `Features.WcsPacks.FourWay`. Traffic guard is Singleton so fallback occupancy survives across scopes.

### Migration

```
dotnet ef migrations add AddFourWayPack --project Seven.Net8/Seven.Infrastructure --startup-project Seven.Net8/Seven.WebApi --output-dir Migrations --context SevenDbContext
```

Tables: `Fw_MapVersion`, `Fw_Node` (`LocationCode` → `Wms_Location.Code` by convention, no FK), `Fw_Route`, `Fw_RouteGroup`, `Fw_ShuttleTask`, `Fw_ShuttleTaskPath`. Guid PK on shuttle task uses `ValueGeneratedNever`.

---

## Self-Review

| Check | Result |
|-------|--------|
| TDD RED→GREEN documented | OK |
| Router A→B on 3-node graph | OK |
| Head-on grant rejected | OK |
| AcceptLeg → `Fw_ShuttleTask` | OK |
| Table prefixes `Fw_` only | OK |
| No Stacker allocators / `Stk_` types in FourWay pack | OK |
| HotStore fallback when disabled | OK |
| Feature-gated DI | OK |
| Git commit | none |

---

## Concerns / Follow-ups

1. **CanHandle is always true** — V1 assumes a registered FourWay pack covers any from/to; dual-pack planning still needs `Wms_HandoverLink` (plan Step 4 not in this task’s 3 tests).
2. **AcceptLeg does not write `Fw_ShuttleTaskPath` or invoke the router** — path persistence needs an active `Fw_MapVersion` + `Fw_Route` seed.
3. **Traffic V1 is exclusive occupancy**, not capacity/`FlowConflictAnalyzer` geometry; same-direction second vehicle also fails.
4. **HotStore grant uses `TryUpdateAsync`**, not `TryAcquireAsync` — Redis get+set is not fully atomic under contention.
5. **Hosted service is a log-only placeholder** — no trigger-port loop (physical comm is Phase H).
6. **No dual-pack Stacker→Handover→FourWay bus test** (plan Step 4).

---

## Out of Scope (confirmed not done)

- Port entire `FlowConflictAnalyzer`
- Stacker allocators / `Stk_` tables
- Real PLC / Socket / Modbus / S7
- Git commit
