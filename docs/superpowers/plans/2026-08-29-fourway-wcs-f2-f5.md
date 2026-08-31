# FourWay WCS F2–F5 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Align FourWay pack with RCS4Shuttle runtime core: three-stage allocation + inbound E2E (F2+F3), then path/outbound/parking/full Hoist (F4+F5). No F6 / DeviceComm.

**Architecture:** Mirror Stacker patterns inside `Fw_` tables and `FourWay*` services. Bus remains thin; pack owns PutAway/Retrieval/Shuttle/Hoist. Simulation via `IEquipmentTriggerPort`. Deliver in two sprints (spec scheme B).

**Tech Stack:** .NET 8, EF Core 8, xUnit + FluentAssertions, HotStore (`fw:`) for traffic in F4

**Spec:** [`docs/superpowers/specs/2026-08-29-fourway-wcs-f2-f5-design.md`](../specs/2026-08-29-fourway-wcs-f2-f5-design.md)

## Global Constraints

- PackId=`fourway`; location codes `Fw.`; never share `Stk_AssignmentPolicy`
- Do not reference RCS4Shuttle assemblies
- Do not implement F6 Promote or real PLC in this plan
- Prefer InMemory tests; use `RemoveRange` not `ExecuteDeleteAsync` where needed
- Commit only when the user explicitly asks

## File map (Sprint 1)

| Path | Role |
|------|------|
| `Seven.Domain/Entities/Wcs/FourWay/FwAislePolicy.cs` | Create |
| `Seven.Domain/Entities/Wcs/FourWay/FwAssignmentRecord.cs` | Create |
| `Seven.Domain/Entities/Wcs/FourWay/FwPutAwayTask.cs` | Create |
| `Seven.Domain/Entities/Wcs/FourWay/FwRequestPoint.cs` | Create |
| `Seven.Domain/Enums/FourWayEnums.cs` | Extend: PutAway status, RequestPoint types |
| `Seven.Domain/Entities/Wcs/FourWay/FwLayerPolicy.cs` | Add AllocationWeight if missing |
| `Seven.Infrastructure/.../FourWayConfigurations.cs` | EF configs |
| `Seven.Infrastructure/.../SevenDbContext.cs` | DbSets |
| Migration `AddFwPolicyPutAwayRequest` | EF |
| `FourWayInboundAllocator.cs` | Rewrite three-stage + book |
| `FourWayLayerAllocator.cs` / `FourWayAisleAllocator.cs` / `FourWayLocationAllocator.cs` | Optional split; may keep logic in InboundAllocator + helpers |
| `FourWayDestinationService.cs` | Create (mirror StackerDestinationService F3 subset) |
| `FourWayWcsPack.cs` | PutAway AcceptLeg + CanHandle Fw. |
| `WcsServiceCollectionExtensions.cs` | DI + HostedService subscribe Destination |
| `Seven.Tests/Wcs/FourWayAllocatorTests.cs` | Create |
| `Seven.Tests/Wcs/InboundToFourWayE2ETests.cs` | Create |
| `doc/24-WCS四向分配与入库.md` | Create |
| `design/shuttle-wcs/02-migration-plan.md` | Tick F2/F3 |

---

## Sprint 1 — F2 + F3

### Task 1: Domain + EF (Policy / PutAway / RequestPoint)

**Files:** entities + enums + configs + DbContext + migration

**Produces:** `FwAislePolicy`, `FwAssignmentRecord`, `FwPutAwayTask`, `FwRequestPoint`, `FwPutAwayStatus`, `FwRequestPointType`, `FwLayerPolicy.AllocationWeight`

- [ ] **Step 1:** Add enums to `FourWayEnums.cs`:

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

- [ ] **Step 2:** Create entities (fields per spec §4): `FwAislePolicy` (LayerCode, AisleCode, MinEmptySlots, MaxShuttleCount, DestinationPointCode, AllocationWeight, IsAvailable, MaxHeight, MaxWeight); `FwAssignmentRecord` (ScopeType Layer|Aisle, ScopeCode, LastAssignedAt, AssignCount); `FwPutAwayTask` (Id Guid, LegId, Container, From/To, Status, AssignedLayer/Aisle/Location); `FwRequestPoint` (Code, PointType, IsEnabled, LayerCode?, AisleCode?). Add `AllocationWeight` default 1 on `FwLayerPolicy`.

- [ ] **Step 3:** EF configs + DbSets + `dotnet ef migrations add AddFwPolicyPutAwayRequest`

- [ ] **Step 4:** Build solution; fix compile errors

---

### Task 2: Three-stage Allocator + Booking (F2)

**Files:** `FourWayInboundAllocator.cs` (+ optional helpers); `FourWayAllocatorTests.cs`

**Consumes:** Task 1 tables  
**Produces:** `AllocateInboundAsync` returns Layer/Aisle/Location and books location

- [ ] **Step 1:** Write failing tests in `FourWayAllocatorTests.cs`:
  - Prefer higher `FwLayerPolicy.AllocationWeight`
  - Skip aisle when empty slots &lt; MinEmptySlots
  - Book sets `WmsLocation.IsBooked`
  - Rotate using `FwAssignmentRecord`

- [ ] **Step 2:** Implement SelectLayer → SelectAisle → SelectLocation + Book in `FourWayInboundAllocator` (replace F1 skeleton). Height/weight filter on policies; layer/aisle availability from `Wms_Layer`/`Wms_Aisle` when present.

- [ ] **Step 3:** Run `dotnet test --filter FullyQualifiedName~FourWayAllocator` — expect PASS

---

### Task 3: PutAway + DestinationService + CanHandle (F3)

**Files:** `FourWayWcsPack.cs`, `FourWayDestinationService.cs`, `FourWaySchedulerHostedService.cs`, DI

**Consumes:** Task 1–2  
**Produces:** SUDR aisle/location assign + segment complete → Bus

- [ ] **Step 1:** `FourWayWcsPack.CanHandleAsync`: true only if both ends start with `Fw.` OR either end is a known handover (if `Wms_HandoverLink` exists for the pair); else false. `AcceptLegAsync`: create `FwPutAwayTask` (inbound RefType) + keep/create `FwShuttleTask` as device carrier **or** defer ShuttleTask until F4 — **Sprint 1 choice:** PutAway is source of truth; create lightweight `FwShuttleTask` Accepted for Leg compatibility with existing pack.

- [ ] **Step 2:** Implement `FourWayDestinationService` mirroring Stacker S1 subset:
  - CheckResult≠OK → Reject + PutAway Failed
  - LayerRequest / AisleRequest → allocate layer/aisle (or read Assigned*) → DispatchDestination(Ep)
  - LocationRequest → SelectLocation+Book → Dispatch(Bin)
  - SegmentFeedback → PutAway Completed → `_bus.OnLegEventAsync(Completed)`

- [ ] **Step 3:** Wire DI: register DestinationService; Scheduler HostedService creates scope, resolves DestinationService.Subscribe() (same pattern as Stacker).

- [ ] **Step 4:** Unit test: NG reject; AisleRequest dispatches EpPoint

---

### Task 4: Inbound E2E + docs (F3验收)

**Files:** `InboundToFourWayE2ETests.cs`, `doc/24`, indexes

- [ ] **Step 1:** E2E: seed WH + Layer + Aisle + Locations `Fw.*` + LayerPolicy + AislePolicy + RequestPoint; BuildPallet via FourWay allocator; Bus+Pack+Destination; SimulateDestinationRequest; SegmentFeedback; assert stock at `Fw.*` target.

- [ ] **Step 2:** Assert pure Stacker leg not accepted by FourWay when both packs registered (CanHandle).

- [ ] **Step 3:** Write `doc/24-WCS四向分配与入库.md`; update `doc/19`, `doc/20`, `doc/README`, `design/shuttle-wcs/02` F2/F3 ✅; mark spec Sprint 1 done when green.

- [ ] **Step 4:** Run  
  `dotnet test --filter "FullyQualifiedName~FourWay|FullyQualifiedName~InboundToFourWay|FullyQualifiedName~InboundToStacker|FullyQualifiedName~StackerPack"`  
  Expect: PASS (Stacker regression green)

---

## Sprint 2 — F4 + F5 (after Sprint 1 green)

### Task 5: PathDispatcher (F4)

- [ ] `FourWayPathDispatcher`: load `Fw_Route` for MapVersion of layer; Dijkstra via `FourWayRouter`; write `Fw_ShuttleTaskPath`; `TrafficGuard.TryGrant` / Release on segment advance
- [ ] Wire Destination SegmentFeedback to advance paths
- [ ] Fallback single segment when no routes
- [ ] Tests: path grant/release; no-map fallback

### Task 6: Retrieval + Parking (F5a)

- [ ] `Fw_RetrievalTask` + group Pri dispatch (mirror Stacker)
- [ ] `Fw_ParkingLedger` reserve/occupy/release
- [ ] Outbound E2E to `Fw.DOCK-*`

### Task 7: Full Hoist (F5b)

- [ ] Entities: `Fw_HoistDevice`, `Fw_HoistLayerPoint`, `Fw_HoistTask`, `Fw_HoistExecTask`
- [ ] Extend `FwRequestPointType` Hoist EP/AP
- [ ] `FourWayHoistOrchestrator`: queue + interlocking; simulation complete via Trigger
- [ ] Cross-layer E2E: source layer → Hoist → dest layer
- [ ] Docs `doc/25`, `doc/26`; update indexes and `design/shuttle-wcs/02`

---

## Spec coverage checklist

| Spec item | Task |
|-----------|------|
| Fw policies + three-stage + book | T1–T2 |
| PutAway + Destination + CanHandle | T3 |
| Inbound E2E + doc/24 | T4 |
| Path + Traffic | T5 |
| Retrieval + Parking | T6 |
| Full Hoist | T7 |
| No F6 / DeviceComm | Global |

## Placeholder / consistency self-review

- No TBD left in Sprint 1 steps  
- Sprint 1 PutAway status names match enum in Task 1  
- Sprint 2 tasks intentionally thinner; expand to TDD detail when Sprint 1 completes  
