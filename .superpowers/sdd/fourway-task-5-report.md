# FourWay WCS Sprint 2 — Task 5 Report

**Task:** PathDispatcher (F4)  
**Date:** 2026-08-29  
**Status:** DONE

---

## Summary

Implemented `FourWayPathDispatcher`: load active/`MapVersion` routes → Dijkstra via `FourWayRouter` → write `Fw_ShuttleTaskPath` → `TrafficGuard.TryGrant`/`Release` on segment advance; no-map/empty-path falls back to single-segment Dispatch (F3-compatible). Wired `FourWayDestinationService` SUDR through PathDispatcher and SegmentFeedback through `AdvanceAfterSegment` (last segment completes PutAway + Bus). Tests green including InboundToFourWay E2E. No git commit.

---

## Deliverables Checklist

| Item | Status |
|------|--------|
| `FourWayPathDispatcher`: Map routes + Dijkstra + path rows + grant/release | Done |
| Destination SegmentFeedback advances multi-node paths; last completes PutAway+Bus | Done |
| Fallback single segment when no routes / empty path | Done |
| Tests: path grant/release; no-map fallback; InboundToFourWay E2E | Done |
| Git commit | Skipped (by policy) |

---

## Files Created

| Path | Description |
|------|-------------|
| `Seven.Net8/Seven.Infrastructure/Wcs/Packs/FourWay/FourWayPathDispatcher.cs` | F4 path dispatch + advance |
| `Seven.Net8/Seven.Tests/Wcs/FourWayPathDispatcherTests.cs` | Grant/release + no-map fallback |
| `.superpowers/sdd/fourway-task-5-report.md` | This report |

## Files Modified

| Path | Change |
|------|--------|
| `FourWayDestinationService.cs` | Inject PathDispatcher; SUDR→Dispatch; SegmentFeedback→Advance then complete |
| `WcsServiceCollectionExtensions.cs` | Register `FourWayPathDispatcher` + Dest ctor |
| `FourWayPackTests.cs` | CreateDest helper with PathDispatcher |
| `InboundToFourWayE2ETests.cs` | Wire PathDispatcher into Dest |
| `.superpowers/sdd/progress-fourway.md` | Task 5 complete |

---

## Behaviour Notes

1. **Map resolve** — Prefer `MapVersionId` on request; else first `IsActive` MapVersion; else first row. No layer↔map field yet.
2. **Path rows** — Seq 1..N node sequence; `EdgeId` = `Fw_Route.Id` for hops (null on start / fallback).
3. **Traffic** — Owner = `ShuttleTaskId` (N format); one edge granted at a time; Advance releases arrived hop then grants next.
4. **Grant fail** — First hop: stay `Routing`, no Dispatch. Mid-path: stay `Routing`, Advance returns true (incomplete).
5. **Fallback** — No map/routes or empty Dijkstra → write From(+To) path without edges, Dispatch final To, first SegmentFeedback completes (E2E unchanged).

---

## Test Summary

```
dotnet test --filter "FullyQualifiedName~FourWay|FullyQualifiedName~InboundToFourWay|FullyQualifiedName~InboundToStacker|FullyQualifiedName~StackerPack|FullyQualifiedName~StackerPath"
→ Passed: 28, Failed: 0
```

| Test | Asserts |
|------|---------|
| `Dispatch_WithMap_ShouldGrantFirstEdge_AndAdvanceReleasesThenGrantsNext` | A→C→B path; grant/release; opposite reject; advance to B then done |
| `Dispatch_WithoutMap_ShouldFallbackSingleSegment` | No edges; Dispatch EP; Advance false |
| `BuildPallet_Allocate_ThenFourWaySimulate_ShouldPutStockAtFwLocation` | Still green (no-map fallback) |
| (+ prior FourWay/Stacker) | Pass |

---

## Concerns / Follow-ups

1. **No layer→MapVersion** — Layers share active map until schema links layer to map.
2. **Grant-blocked mid-path** — No HostedService retry yet; shuttle stays Routing.
3. **F5** — Retrieval/Parking/Hoist still open.

---

## Spec Coverage

| Spec §5.1 | Status |
|-----------|--------|
| PathDispatcher + Fw_ShuttleTaskPath + 占边分段 | Done |
| 无边/无 Map 单段回退 | Done |
