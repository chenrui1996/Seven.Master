# FourWay WCS Sprint 2 — Task 6 Report

**Task:** Retrieval + Parking (F5a)  
**Date:** 2026-08-29  
**Status:** DONE

---

## Summary

Implemented `Fw_RetrievalTask` (group Pri dispatch mirroring Stacker) and `Fw_ParkingLedger` (Free/Reserved/Occupied). Outbound `AcceptLeg` creates Retrieval + Shuttle; `TryDispatchRetrieval` reserves parking then PathDispatcher; SegmentFeedback completes Retrieval, releases parking, rolls next Pri. OutboundToFourWay E2E green. No git commit.

---

## Deliverables Checklist

| Item | Status |
|------|--------|
| `Fw_RetrievalTask` + group Pri dispatch | Done |
| `Fw_ParkingLedger` reserve on dispatch / release on complete | Done |
| Destination completes Retrieval + Pri roll | Done |
| Outbound E2E to `Fw.DOCK-*` | Done |
| Same-group Pri order + parking tests | Done |
| Migration `AddFwRetrievalAndParking` | Done |
| Git commit | Skipped (by policy) |

---

## Files Created

| Path | Description |
|------|-------------|
| `Seven.Domain/Entities/Wcs/FourWay/FwRetrievalTask.cs` | 出库取货任务 |
| `Seven.Domain/Entities/Wcs/FourWay/FwParkingLedger.cs` | 停车账本 |
| `Seven.Tests/Wcs/OutboundToFourWayE2ETests.cs` | 出库 E2E + Pri + 无车挂起 |
| `Persistence/Migrations/20260829122322_AddFwRetrievalAndParking.*` | EF migration |
| `.superpowers/sdd/fourway-task-6-report.md` | This report |

## Files Modified

| Path | Change |
|------|--------|
| `FourWayEnums.cs` | `FwRetrievalStatus`, `FwParkingStatus` |
| `FourWayConfigurations.cs` | EF configs |
| `SevenDbContext.cs` | DbSets |
| `FourWayWcsPack.cs` | Outbound Accept + TryDispatch + parking |
| `FourWayDestinationService.cs` | Retrieval complete + Pri roll + pack inject |
| `WcsServiceCollectionExtensions.cs` | Pack factory + Dest pack arg |
| `.superpowers/sdd/progress-fourway.md` | Task 6 complete |

---

## Behaviour Notes

1. **AcceptLeg** — `OutboundOrder` / `FourWayTransfer` → Retrieval + Shuttle（无 PutAway）；入库仍 PutAway + Shuttle。
2. **Pri** — 同组更小 Pri 未终态则 Suspended；完成后 Destination 滚动下一单。
3. **Parking** — 派发前抢 Free→Reserved（OwnerId=Retrieval.Id）；完成/取消→Free；无 Free 则 Suspended。
4. **Path** — 出库直接 From→To 经 PathDispatcher（无 SUDR）；末段反馈完成 Bus。

---

## Test Summary

```
dotnet test --filter "FullyQualifiedName~FourWay|...|OutboundToFourWay|OutboundToStacker|..."
→ Passed: 33, Failed: 0
```

| Test | Asserts |
|------|---------|
| `OutboundApprove_Retrieval_ShouldMoveStockToDock` | Retrieval→Dock；停车预订/释放；库存到位 |
| `SameGroup_ShouldDispatchByWcsPriOrder` | Pri=1 先发；完成后滚 Pri=2 |
| `NoFreeParking_ShouldSuspendRetrieval` | 无空闲停车挂起 |
| (+ prior FourWay/Stacker) | Pass |

---

## Concerns / Follow-ups

1. **停车位与层/巷未绑定** — 全局 Free 池；F5b/容量策略可按层收紧。
2. **无 HostedService 重试挂起出库** — 停车释放后仅同组滚动会再派；跨组挂起需调度轮询（可后续加）。
3. **F5b Hoist** — 跨层仍开放。

---

## Spec Coverage

| Spec §5.2 | Status |
|-----------|--------|
| Fw_RetrievalTask WcsGroupNo/Pri 滚动 | Done |
| Fw_ParkingLedger 预订/占用/释放 | Done（Reserved↔Free；Occupied 枚举已备） |
| 派车：空闲车+账本；无车挂起 | Done |
