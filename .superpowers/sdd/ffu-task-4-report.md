# FFU Task 4 Report — FU4 Hoist harden + docs

**Status:** Done  
**Date:** 2026-08-29  
**Branch:** feature/wms-wcs-pack (no commit)

## Spec coverage

| Spec §6 | Result |
|---------|--------|
| Startup/periodic Suspended Exec scan | ✅ `ScanAndWakeQueuedExecsAsync` + HostedService startup + tick |
| SUDR Hoist*/Shuttle* no false Reject | ✅ DestinationService early return |
| Multi-hoist prefer Available + free port | ✅ `SelectHoistPortsAsync` scoring |
| Outbound RefType → Outbound Ap/Ep | ✅ `OutboundOrder`; else Inbound; cross-fallback |
| FeedbackCode≠OK no stage advance | ✅ ToSrcAp/FromDesEp stay; HoistLift → Failed |
| doc/27 + indexes | ✅ 19/20/25/26/README + design/shuttle-wcs/02 |

## Changes

1. **`FourWayHoistOrchestrator`**: port selection by RefType + multi-hoist score; scan/wake; FeedbackCode gate on all stages.
2. **`FourWayDestinationService`**: Hoist*/Shuttle* SUDR OK/NG → no Reject.
3. **`FourWaySchedulerHostedService`**: startup + periodic hoist scan with path/parking.
4. **Docs**: `doc/27-WCS四向硬化与跟进.md`; ticks on 19/20/25/26/README; `design/shuttle-wcs/02` FFU row.

## Tests

| Test | Result |
|------|--------|
| `ScanAndWake_SuspendedExec_ShouldDispatch` | Pass |
| `OutboundOrder_ShouldUseOutboundApEp` | Pass |
| `BadFeedback_ShouldNotAdvanceHoistStage` | Pass |
| `HoistSudrPoint_ShouldNotFalseReject` | Pass |
| `MultiHoist_ShouldPreferAvailableFreePort` | Pass |
| Existing Hoist E2E / same-port | Pass |

Filter: `FourWay|StackerPack|InboundTo*|OutboundTo*` → **54/54 pass**.

## Concerns

- SUDR Hoist/Shuttle types only silence Reject; full RCS destination handshake not implemented (per spec).
- Outbound ports keyed only on `OutboundOrder` (not `FourWayTransfer`, which keeps Inbound for cross-layer E2E).
