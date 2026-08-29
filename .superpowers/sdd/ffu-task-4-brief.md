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
