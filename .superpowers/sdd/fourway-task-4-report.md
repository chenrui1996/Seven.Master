# FourWay WCS Sprint 1 — Task 4 Report

**Task:** Inbound E2E + docs (F3验收)  
**Date:** 2026-08-29  
**Status:** DONE

---

## Summary

Delivered full FourWay inbound E2E (BuildPallet → SUDR → SegmentFeedback → stock at `Fw.*`), dual-pack CanHandle assertion, Stacker I1 fix (ignore enabled `FwRequestPoint` on shared TriggerPort), and product doc `doc/24` with index / migration-plan ticks. Filter tests green (24 passed) including Stacker regression. No git commit.

---

## Deliverables Checklist

| Item | Status |
|------|--------|
| `InboundToFourWayE2ETests`: seed + BuildPallet + Bus + Dual Dest + stock @ Fw.* | Done |
| Dual-pack: FourWay rejects pure `Stk.*` CanHandle | Done |
| StackerDestinationService I1: Fw RP → return without Reject | Done |
| `doc/24-WCS四向分配与入库.md` | Done |
| Update `doc/19`, `doc/20`, `doc/README`, `design/shuttle-wcs/02` F2/F3 ✅ | Done |
| Spec Sprint 1 marked done | Done |
| Filter tests PASS (Stacker regression) | Done (24) |
| Git commit | Skipped (by policy) |

---

## Files Created

| Path | Description |
|------|-------------|
| `Seven.Net8/Seven.Tests/Wcs/InboundToFourWayE2ETests.cs` | Inbound E2E + dual-pack CanHandle |
| `doc/24-WCS四向分配与入库.md` | F2/F3 product doc |
| `.superpowers/sdd/fourway-task-4-report.md` | This report |

## Files Modified

| Path | Change |
|------|--------|
| `StackerDestinationService.cs` | I1: unknown Stk RP but enabled Fw RP → silent return |
| `doc/19-WMS与WCS包.md` | Link to doc/24 |
| `doc/20-WMS迁入完整实现.md` | F2/F3 status + §10.3 + related docs |
| `doc/README.md` | Index row for doc/24 |
| `design/shuttle-wcs/02-migration-plan.md` | F2/F3 ✅ |
| `docs/superpowers/specs/2026-08-29-fourway-wcs-f2-f5-design.md` | Sprint ① done |
| `.superpowers/sdd/progress-fourway.md` | Task 4 complete |

---

## Behaviour Notes

1. **E2E dual DestinationService** — both FourWay and Stacker subscribe; Fw AisleRequest must Dispatch Ep with **zero** Rejects.
2. **Bus single-pack** — OrchestrationBus multi-pack planning still requires HandoverLink; E2E registers only FourWay on Bus (Stacker pack only for CanHandle assert).
3. **F3 single segment** — no path dispatcher; first SegmentFeedback completes PutAway → Bus → WMS.

---

## Test Summary

```
dotnet test --filter "FullyQualifiedName~FourWay|FullyQualifiedName~InboundToFourWay|FullyQualifiedName~InboundToStacker|FullyQualifiedName~StackerPack"
→ Passed: 24, Failed: 0
```

| Test | Asserts |
|------|---------|
| `BuildPallet_Allocate_ThenFourWaySimulate_ShouldPutStockAtFwLocation` | Target Fw.LOC; Dispatch EP-Fw.A1; no Reject; stock/PutAway Completed |
| `DualPack_FourWay_ShouldNotAccept_PureStackerLeg` | Stk pair false; Fw pair true |
| (+ prior FourWay/Stacker/InboundToStacker) | Pass |

---

## Concerns / Follow-ups

1. **Bus multi-pack same-route** — PlanLegs still handover-only when ≥2 packs; pure Fw↔Fw with both packs registered needs PlanLegs enhancement or single-pack registration.
2. **Stacker NG before RP lookup** — CheckResult≠OK still Rejects before Fw-point check; dual-pack NG may double-Reject (OK path verified).
3. **F4/F5** — path/outbound/hoist still open (Sprint ②).

---

## Report Path

`Seven.Master/.superpowers/sdd/fourway-task-4-report.md`
