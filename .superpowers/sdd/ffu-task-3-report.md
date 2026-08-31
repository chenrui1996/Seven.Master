# FFU Task 3 Report — FU3 Parking / MaxShuttle

**Status:** Done (+ I1–I3 FFU review fixes)  
**Date:** 2026-08-29  
**Branch:** feature/wms-wcs-pack (no commit)

## Re-review (post I1–I3)

**Spec:** ✅  
**Quality:** Approved

| 原 Important | 复验结论 |
|--------------|----------|
| **I1** DispatchAsync 异常 → 孤儿 Reserved | ✅ `try/catch` 释位 + 释边 + `Suspend`；`Dispatch_WhenPortThrows_ShouldReleaseParkingAndSuspend` 通过 |
| **I2** 占边失败仍 Occupied/Dispatched | ✅ 仅 `shuttle.Status == Running` 时晋升；`Routing` 保持 Reserved；HostedService 在 `RetryStuckRouting` 后调用 `PromoteRetrievalsAfterPathGrantAsync`；I2 单测通过 |
| **I3** MaxShuttle scope 未解析 | ✅ `ResolveParkingScopeAsync`（From/To 库位 + AisleId 回退 + 停车 Location + FwRequestPoint）+ `EnsureParkingScopeAsync` 回填；I3 单测通过 |

**Tests (re-run):** `FourWayParkingTests|OutboundToFourWayE2ETests|FourWayAllocatorTests` → **15/15 pass**

### Critical

无

### Important (remaining)

**[I4] 并发原子性测试未覆盖关系库** — `FourWayParkingTests.cs`

`ConcurrentReserve_ShouldNotDoubleOccupySameSpot` 仍仅用 InMemory + 进程内 `SemaphoreSlim`，未验证生产路径 `ExecuteUpdate WHERE Status=Free` 在 MySQL 下的并发安全。实现已就位；属测试缺口，本 FFU 竖切已明确 defer。

---

## Changes

1. **Atomic parking reserve** (`TryReserveParkingAsync`): relational DB uses `ExecuteUpdate` with `WHERE Status=Free`; InMemory uses process gate + Free check. Stamps `LayerCode`/`AisleCode` from From location.

2. **Reserved→Occupied** on successful path dispatch (`MarkParkingOccupiedAsync`); also on mid-path SegmentFeedback. **Free** on complete/cancel/fail (existing `ReleaseParkingAsync`).

3. **`WakeSuspendedRetrievalsAsync`**: HostedService periodic scan (~3s) wakes Suspended Retrievals when Free parking exists.

4. **Allocator MaxShuttleCount**: skip aisle when that layer/aisle `Reserved+Occupied` ≥ `MaxShuttleCount` (>0).

5. **Migration** `AddFwParkingLayerAisle`: nullable `LayerCode`/`AisleCode` + index on ParkingLedger.

## FFU review fixes (I1–I3)

| ID | Fix |
|----|-----|
| **I1** | After reserve, `DispatchAsync` wrapped in try/catch; on exception → `ReleaseParking` (+ best-effort edge release) + `Suspend` (no orphan Reserved). |
| **I2** | `MarkParkingOccupied` + Retrieval `Dispatched` only when shuttle is `Running` after dispatch. Grant fail (`Routing`) keeps `Reserved`. HostedService calls `PromoteRetrievalsAfterPathGrantAsync` after `RetryStuckRouting`. |
| **I3** | Reserve stamps Layer/Aisle from Retrieval `FromCode` WmsLocation (AisleId fallback), then ToCode / parking `LocationCode` / enabled `FwRequestPoint`; backfill on re-reserve. |

## Tests

| Test | Result |
|------|--------|
| `ConcurrentReserve_ShouldNotDoubleOccupySameSpot` | Pass |
| `WakeSuspended_AfterFreeParking_ShouldDispatch` | Pass |
| `SelectAisle_ShouldSkip_WhenMaxShuttleReached` | Pass |
| `Dispatch_ShouldMarkParkingOccupied_ReleaseOnComplete` | Pass |
| `Dispatch_WhenPortThrows_ShouldReleaseParkingAndSuspend` (I1) | Pass |
| `Dispatch_WhenGrantBlocked_ShouldKeepReservedNotOccupiedOrDispatched` (I2) | Pass |
| `Reserve_ShouldStampLayerAisle_FromRetrievalFromLocation` (I3) | Pass |
| OutboundToFourWay E2E (Occupied after dispatch) | Pass |
| FourWayAllocatorTests | Pass |

Filter: `FourWayParkingTests|OutboundToFourWayE2ETests|FourWayAllocatorTests` → **15/15**.

## Concerns

- InMemory cannot translate `ExecuteUpdate`; gate is process-local only (multi-instance relies on relational conditional update).
- MaxShuttle counts parking rows stamped with Layer/Aisle at reserve time; legacy Free rows without scope do not inflate counts until reserved.
- I4 (relational concurrent reserve test) still open — not in this FFU pass.
