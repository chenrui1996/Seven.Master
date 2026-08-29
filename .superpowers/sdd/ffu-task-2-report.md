# FFU Task 2 Report — FU2 Path harden

**Status:** Done (+ Important fixes)  
**Date:** 2026-08-29  
**Branch:** feature/wms-wcs-pack (no commit)

## Changes

1. **`Fw_MapVersion.LayerCode`** (+ EF migration `AddFwMapVersionLayerCode`): nullable layer binding; `ResolveMapVersionId` = explicit Id → LayerCode → Active → first.
2. **`FourWayPathDispatcher`**: request carries `LayerCode`; `RetryStuckRoutingAsync` / `TryResumeGrantAsync` for Routing without grant; `ReleaseAllEdgesAsync` for Cancel/Fail; advance updates `FromCode` for mid-path resume.
3. **`FourWaySchedulerHostedService`**: BackgroundService periodic grant retry (~3s) + existing Dest/Feedback subscriptions.
4. **Release on Cancel/Fail**: `CancelLeg` / `FailLeg` and PutAway/Retrieval `Reject(markFailed)` release all shuttle-owned flow edges.
5. **Layer context**: PutAway `AssignedLayer`, Retrieval via hoist layer resolve, Hoist `SrcLayer`/`DesLayer` passed into Dispatch.

## Important fixes (post-review)

1. **HotStore cancel release**: `FourWayTrafficGuard` keeps a process-local owner→edgeIds index (HotStore cannot enumerate keys). `ReleaseOwnedEdgesAsync` / `ReleaseAllForOwnerAsync` merge path EdgeIds + owner index. Grant always writes EdgeId on path rows before occupy; Cancel/Fail release those ids.
2. **Fail release**: `FailLegAsync` marks PutAway/Retrieval/Shuttle Failed + `ReleaseAllEdgesAsync`; `Reject(markFailed)` also fails active Retrieval and releases edges.
3. **`TryResumeGrantAsync` idempotency**: Running → skip; alreadyHeld while Routing → sync Running, **no** re-Dispatch.

## Tests

| Test | Result |
|------|--------|
| `RetryStuckRouting_AfterGrantFreed_ShouldDispatch` | Pass |
| `CancelLeg_ShouldReleaseAllFlowEdges` | Pass |
| `RejectMarkFailed_ShouldReleaseAllFlowEdges` | Pass |
| `FailLeg_Retrieval_ShouldReleaseAllFlowEdges` | Pass |
| `TryResumeGrant_WhenAlreadyRunning_ShouldNotReDispatch` | Pass |
| `TryResumeGrant_WhenAlreadyHeldButRouting_ShouldSkipReDispatch` | Pass |
| `TwoLayers_TwoMaps_ShouldIsolateRoutesByLayerCode` | Pass |
| Existing path grant/advance + no-map fallback | Pass |
| FourWayPackTests subset | Pass |

Filter: `FourWayPathDispatcherTests|FourWayPackTests` → **15/15 pass**.

## Concerns

- HotStore still cannot SCAN keys; owner index is in-process only (lost on restart). Durable recovery relies on path-row EdgeIds written at Grant time.
- Mid-path grant retry relies on `FromCode` updated at segment arrive; initial stuck still uses start FromCode.
