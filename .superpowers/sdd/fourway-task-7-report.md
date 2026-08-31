# FourWay WCS Sprint 2 — Task 7 Report

**Task:** Full Hoist (F5b)  
**Date:** 2026-08-29  
**Status:** DONE

---

## Summary

Implemented `Fw_HoistDevice` / `Fw_HoistLayerPoint` / `Fw_HoistTask` / `Fw_HoistExecTask` with migration `AddFwHoist`. `FourWayHoistOrchestrator` drives cross-layer as **single Bus Leg + pack-internal stages** (ToSrcAp → HoistLift → FromDesEp); same-port interlocking suspends later Execs; simulation completes via reused `SegmentFeedback`. Docs `doc/25`–`doc/26` and indexes updated. No git commit.

---

## Deliverables Checklist

| Item | Status |
|------|--------|
| Entities + enums + EF + `AddFwHoist` | Done |
| Extend `FwRequestPointType` Hoist/Shuttle EP·AP | Done |
| `FourWayHoistOrchestrator` queue + interlocking + Trigger | Done |
| Cross-layer E2E stock at dest `Fw.*` | Done |
| Same-port conflict suspends second | Done |
| Docs 25/26 + doc/19,20,README + design/02 | Done |
| Git commit | Skipped (by policy) |

---

## Cross-layer approach

**Single Bus order/Leg**, pack-internal stages (not multi-leg Bus):

1. Source shuttle: From → src layer Hoist AP (`InboundAp`)
2. HoistExec: DispatchDestination to dest EP; SegmentFeedback completes lift
3. Dest shuttle: EP → To; final feedback → Bus Complete → WMS stock move

---

## Files Created

| Path | Description |
|------|-------------|
| `FwHoistDevice/LayerPoint/Task/ExecTask.cs` | Domain |
| `FourWayHoistOrchestrator.cs` | Queue + stages + interlocking |
| `FourWayHoistE2ETests.cs` | Cross-layer + same-port |
| `Persistence/Migrations/20260829123227_AddFwHoist.*` | EF |
| `doc/25-WCS四向寻路与出库.md` | F4/F5a |
| `doc/26-WCS四向提升机.md` | F5b |
| `.superpowers/sdd/fourway-task-7-report.md` | This report |

## Files Modified

| Path | Change |
|------|--------|
| `FourWayEnums.cs` | Hoist statuses/stages + RequestPoint types |
| `FourWayConfigurations.cs` / `SevenDbContext.cs` | EF |
| `FourWayWcsPack.cs` | Cross-layer AcceptLeg |
| `FourWayDestinationService.cs` | Hoist feedback first |
| `WcsServiceCollectionExtensions.cs` | DI |
| `doc/19,20,README` / `design/shuttle-wcs/02` | F4/F5 ticks |

---

## Test Summary

```
dotnet test --filter "FullyQualifiedName~FourWay|...|OutboundToFourWay|...|Stacker..."
→ Passed: 37, Failed: 0
```

| Test | Asserts |
|------|---------|
| `CrossLayer_Transfer_ShouldMoveStockViaHoistStages` | 3 stages; stock @ dest Fw.* |
| `SamePort_SecondExec_ShouldSuspendUntilFirstCompletes` | 2nd Suspended → Dispatched after 1st |
| (+ prior FourWay/Stacker) | Pass |

---

## Concerns / Follow-ups

1. **层口点位选用** — 跨层默认源 `InboundAp`、目标 `InboundEp`；出库向口可再按业务类型分支。
2. **SUDR 类型已扩枚举** — Hoist/Shuttle EP·AP 尚未在 DestinationService 申请分支消费。
3. **无 HostedService 重试挂起 Hoist** — 依赖前单完成唤醒；进程重启后需调度补扫。

---

## Spec Coverage

| Spec §5.3–5.4 | Status |
|---------------|--------|
| Fw_Hoist* tables | Done |
| Queue Pri/CreateTime + same-port suspend | Done |
| Cross-layer E2E | Done |
| SegmentFeedback simulation | Done |
