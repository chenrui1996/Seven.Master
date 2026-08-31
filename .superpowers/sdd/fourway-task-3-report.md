# FourWay WCS Sprint 1 — Task 3 Report

**Task:** PutAway + DestinationService + CanHandle (F3)  
**Date:** 2026-08-29  
**Status:** DONE

---

## Summary

Implemented `FourWayDestinationService` (SUDR NG reject / Layer·Aisle→Ep / Location→Book+Bin / single-segment complete→Bus), narrowed `FourWayWcsPack.CanHandle` to `Fw.` or handover, AcceptLeg writes `FwPutAwayTask` + lightweight `FwShuttleTask`, and wired DI + HostedService like Stacker. Unit tests: NG reject + AisleRequest→Ep. No full inbound E2E (Task 4).

---

## Deliverables Checklist

| Item | Status |
|------|--------|
| `CanHandle`: both `Fw.` OR handover link; reject pure `Stk.*` | Done |
| AcceptLeg: PutAway source of truth + Shuttle Accepted | Done |
| DestinationService: NG→Reject+Failed | Done |
| Layer/AisleRequest → allocate → Dispatch Ep | Done |
| LocationRequest → Select+Book → Dispatch Bin | Done |
| SegmentFeedback → PutAway Completed → Bus | Done (F3 single segment, no path) |
| DI + HostedService scope→Handle | Done |
| Unit: NG reject; AisleRequest→Ep | Done |
| Git commit | Skipped (by policy) |
| Full inbound E2E | Out of scope (Task 4) |

---

## Files Created

| Path | Description |
|------|-------------|
| `Seven.Net8/Seven.Infrastructure/Wcs/Packs/FourWay/FourWayDestinationService.cs` | SUDR + SegmentFeedback |
| `Seven.Master/.superpowers/sdd/fourway-task-3-report.md` | This report |

## Files Modified

| Path | Change |
|------|--------|
| `FourWayWcsPack.cs` | CanHandle Fw./handover; PutAway+Shuttle AcceptLeg; Query/Cancel prefer PutAway |
| `FourWaySchedulerHostedService.cs` | Port events → scoped DestinationService (Stacker pattern) |
| `FourWayInboundAllocator.cs` | Public stage APIs for DestinationService |
| `WcsServiceCollectionExtensions.cs` | Register `FourWayDestinationService` |
| `FourWayPackTests.cs` | CanHandle/PutAway asserts; NG reject; AisleRequest→Ep |

---

## Behaviour Notes

1. **Unknown FwRequestPoint** — early return (no reject), so Stacker SUDR points are not poisoned by FourWay.
2. **Warehouse resolve** — from PutAway To/From/Assigned location, else AssignedLayer master, else warehouse with `fourway` in EnabledPackIds.
3. **Assigned\*** — reused when already set; otherwise SelectLayer → SelectAisle → Ep from `FwAislePolicy.DestinationPointCode`.
4. **F3 path** — no `FourWayPathDispatcher`; DispatchDestination only; first SegmentFeedback completes PutAway+Shuttle and raises Bus Completed.

---

## Test Summary

```
dotnet test --filter FullyQualifiedName~FourWay
→ Passed: 13, Failed: 0
```

| Test | Asserts |
|------|---------|
| `AcceptLeg_ShouldCreatePutAwayAndShuttle_WhenCanHandle` | PutAway+Shuttle Accepted; Stk pair false |
| `CanHandle_ShouldAllow_HandoverEnd` | HO+Fw true; bare RECV+Fw false |
| `SimulateDestinationRequest_CheckNg_ShouldReject` | Reject reason; PutAway Failed |
| `AisleRequest_ShouldDispatchEpPoint` | Ep=`EP-Fw.A1`; Layer/Aisle assigned |
| (+ Allocator 5 + Router/Traffic) | Pass |

---

## Concerns / Follow-ups

1. **Shared trigger port** — Stacker still rejects unknown request points; a FourWay-only RP may be rejected by Stacker HostedService when both packs run. Task 4 E2E / later should namespace points or make Stacker ignore foreign RPs (mirror FourWay early-return).
2. **Outbound** — AcceptLeg skips PutAway for OutboundOrder/FourWayTransfer; full outbound is F4/F5.
3. **Task 4** — inbound E2E + docs + dual-pack CanHandle regression still pending.

---

## Report Path

`Seven.Master/.superpowers/sdd/fourway-task-3-report.md`
