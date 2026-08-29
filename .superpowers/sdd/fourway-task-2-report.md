# FourWay WCS Sprint 1 — Task 2 Report

**Task:** Three-stage Allocator + Booking (F2)  
**Date:** 2026-08-29  
**Status:** DONE

---

## Summary

Rewrote `FourWayInboundAllocator.AllocateInboundAsync` as SelectLayer → SelectAisle → SelectLocation + Booking, using `FwLayerPolicy` / `FwAislePolicy` / `FwAssignmentRecord` (never `Stk_AssignmentPolicy`). Added `FourWayAllocatorTests` (4 cases). Filtered test run passes.

---

## Deliverables Checklist

| Item | Status |
|------|--------|
| Prefer higher `FwLayerPolicy.AllocationWeight` | Done |
| Skip aisle when empty &lt; `MinEmptySlots` | Done |
| Book sets `WmsLocation.IsBooked` | Done |
| Rotate via `FwAssignmentRecord` (Layer scope) | Done |
| Height/weight filter on policies | Done |
| `Wms_Layer` / `Wms_Aisle` availability when present | Done |
| `dotnet test --filter FullyQualifiedName~FourWayAllocator` PASS | Done (4/4) |
| Git commit | Skipped (by policy) |
| DestinationService / PutAway AcceptLeg | Out of scope (Task 3) |

---

## Files Created

| Path | Description |
|------|-------------|
| `Seven.Net8/Seven.Tests/Wcs/FourWayAllocatorTests.cs` | TDD: weight, MinEmptySlots, booking, layer rotation |

## Files Modified

| Path | Change |
|------|--------|
| `Seven.Net8/Seven.Infrastructure/Wcs/Packs/FourWay/FourWayInboundAllocator.cs` | Replaced F1 stub with three-stage + booking |

---

## Algorithm

1. **Layer** — Prefer `PreferredLayerCode` if set (validate `WmsLayer`); else filter `FwLayerPolicy` by warehouse code, zone (optional), IsAvailable, height/weight; respect `WmsLayer.IsAvailable` when master rows exist; order by `AllocationWeight` desc → `FwAssignmentRecord` (ScopeType=Layer) `LastAssignedAt` → code; touch record.
2. **Aisle** — Prefer `PreferredAisleCode`; else filter `FwAislePolicy` for layer + height/weight + IsAvailable; skip if `WmsAisle` unavailable; skip if location master exists and empty slots &lt; `MinEmptySlots`; rotate via ScopeType=Aisle with scope `{layer}/{aisle}`. If **no** aisle policies for the layer, fall back to aisle of first free location (keeps PreferredLayer-only PackPrefix path).
3. **Location** — First free `WmsLocation` (pack fourway, warehouse, aisle, optional LayerId) ordered by Code; set `IsBooked=true` and save.

`MaxShuttleCount` is not enforced yet (no shuttle occupancy ledger until F5 parking).

---

## Test Summary

```
dotnet test --filter FullyQualifiedName~FourWayAllocator
→ Passed: 4, Failed: 0
```

| Test | Asserts |
|------|---------|
| `SelectLayer_ShouldPrefer_HigherAllocationWeight` | L02 (weight 10) over L01 (1) |
| `SelectAisle_ShouldSkip_WhenMinEmptySlotsNotMet` | MinEmpty=2 with 1 empty → Ok=false |
| `SelectLocation_ShouldBook_IsBooked` | target `IsBooked` |
| `SelectLayer_ShouldRotate_UsingFwAssignmentRecord` | L01↔L02 alternate; 2 Layer records |

Regression check: `PackPrefixAndMultiPackTests.SameWarehouse_ShouldHost_StackerAndFourWayLocations` still passes (PreferredLayer + no aisle policy fallback).

---

## Concerns / Follow-ups

1. **MaxShuttleCount** — Field exists on `FwAislePolicy` but F2 does not count live shuttles; tighten in F5 with parking ledger.
2. **Aisle ScopeCode** — Uses `{LayerCode}/{AisleCode}` to avoid cross-layer collisions; Task 3 DestinationService should use the same key if it updates records.
3. **No-policy path** — Full auto-allocate without `FwLayerPolicy` fails with「无可用层策略」; only PreferredLayer without aisle policies still works via location fallback.
4. Task 3 should consume booked location / Assigned* on PutAway AcceptLeg + DestinationService.

---

## Report Path

`Seven.Master/.superpowers/sdd/fourway-task-2-report.md`

---

## Task 2 Review Fix (2026-08-29)

**Status:** FIXED  
**Scope:** Layer-scoped aisle master / MinEmptySlots (Important findings)

### Changes

1. **`SelectAisleAsync` — `WmsAisles`**  
   Resolve current `WmsLayer` by `layerCode`, then filter aisles with `LayerId == layer.Id` before `ToDictionary`, so shared aisle codes across layers no longer collide or pick the wrong availability.

2. **`SelectAisleAsync` — empty counts / location master**  
   `emptyCounts` and `hasLocationMaster` now restrict to the selected layer (`LayerId == null || LayerId == layer.Id`), so `MinEmptySlots` does not count empties from other layers with the same aisle code string.

3. **Regression test**  
   `SelectAisle_ShouldUseSelectedLayerOnly_WhenAisleCodeSharedAcrossLayers`: two layers reuse `Fw.A-SHARED`; L02 empties + unavailable aisle must not satisfy/block L01. Seed helper looks up `WmsAisle` by code **and** `LayerId`.

4. **Unchanged**  
   `MaxShuttleCount` remains deferred (no shuttle occupancy ledger until F5).

### Test results

```
dotnet test --filter FullyQualifiedName~FourWayAllocator
→ Passed: 5, Failed: 0
```

| Test | Result |
|------|--------|
| `SelectLayer_ShouldPrefer_HigherAllocationWeight` | Pass |
| `SelectAisle_ShouldSkip_WhenMinEmptySlotsNotMet` | Pass |
| `SelectLocation_ShouldBook_IsBooked` | Pass |
| `SelectAisle_ShouldUseSelectedLayerOnly_WhenAisleCodeSharedAcrossLayers` | Pass (new) |
| `SelectLayer_ShouldRotate_UsingFwAssignmentRecord` | Pass |

Git commit: skipped (by policy).
