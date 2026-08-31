# FourWay WCS Follow-up (WCS-FFU) Implementation Plan

> **For agentic workers:** Use subagent-driven-development or executing-plans. Checkbox tracking. No git commit unless user asks.

**Goal:** Harden FourWay F2–F5 follow-ups: dual-pack Trigger, Path retry/release/Layer↔Map, Parking concurrency/Occupied/MaxShuttle, Hoist scan/SUDR/ports/error feedback.

**Architecture:** Vertical slices FU1→FU4; extend existing FourWay/Stacker services and HostedService polling; no F6/DeviceComm.

**Tech Stack:** .NET 8, EF Core, xUnit/FluentAssertions

**Spec:** [`docs/superpowers/specs/2026-08-29-fourway-wcs-ffu-design.md`](../specs/2026-08-29-fourway-wcs-ffu-design.md)

## Global Constraints

- Do not add F6 / DeviceComm / RCS assemblies
- Do not share Stk_AssignmentPolicy
- Prefer InMemory tests; no auto-commit
- Keep existing FourWay/Stacker E2E green

---

### Task 1: FU1 Dual-pack Trigger NG silence

**Files:** `StackerDestinationService.cs`, `FourWayDestinationService.cs`, DualPack / new tests

- [x] Stacker: on CheckResult≠OK, if enabled `FwRequestPoint` matches SourcePointCode → return (no Reject)
- [x] FourWay: symmetric for enabled `StkRequestPoint`
- [x] Remove DualPack dead `WcsPackResolver` line
- [x] Tests: Fw RP+NG → only FourWay Reject; Stk RP+NG → only Stacker Reject
- [x] Report `.superpowers/sdd/ffu-task-1-report.md`

---

### Task 2: FU2 Path harden

**Files:** `FwMapVersion.cs` (+ LayerCode), migration, `FourWayPathDispatcher.cs`, `FourWayWcsPack.CancelLeg`, `FourWaySchedulerHostedService` or Path retry helper, tests

- [x] Add `Fw_MapVersion.LayerCode`; ResolveMap: layer → active → first
- [x] Retry grant for Routing tasks stuck without grant (scheduler tick)
- [x] Cancel/Fail release all edges for shuttle ownerId
- [x] Tests: retry after grant free; cancel clears flow; two layers two maps
- [x] Report `.superpowers/sdd/ffu-task-2-report.md`

---

### Task 3: FU3 Parking / MaxShuttle

**Files:** `FourWayWcsPack` parking helpers, DestinationService Occupied transition, Allocator MaxShuttleCount, HostedService wake Suspended retrievals, tests

- [x] Atomic reserve (update where Free)
- [x] Reserved→Occupied on dispatch/segment start; Free on complete/cancel
- [x] Scheduler: wake parking-suspended Retrievals
- [x] Allocator: skip aisle when Reserved+Occupied ≥ MaxShuttleCount
- [x] Tests: concurrent reserve; wake after free; MaxShuttle skip
- [x] Report `.superpowers/sdd/ffu-task-3-report.md`

---

### Task 4: FU4 Hoist harden + docs

**Files:** `FourWayHoistOrchestrator`, `FourWayDestinationService` SUDR Hoist types, HostedService hoist scan, RefType→Inbound/Outbound ports, error FeedbackCode gate, `doc/27`, update 19/20/25/26/README, design/shuttle-wcs/02

- [x] Startup/periodic scan Suspended Exec
- [x] SUDR Hoist*/Shuttle* types: no false Reject; advance/complete as designed
- [x] Multi-hoist: prefer available device + free port
- [x] Outbound RefType → Outbound Ap/Ep
- [x] FeedbackCode≠OK → do not advance stage
- [x] `doc/27-WCS四向硬化与跟进.md` + index ticks
- [x] Tests: scan wake; outbound ports; bad feedback no advance
- [x] Filter regression: FourWay|StackerPack|InboundTo*|OutboundTo*
- [x] Report `.superpowers/sdd/ffu-task-4-report.md`

---

## Spec coverage

| Spec | Task |
|------|------|
| FU1 | T1 |
| FU2 | T2 |
| FU3 | T3 |
| FU4 + doc/27 | T4 |
