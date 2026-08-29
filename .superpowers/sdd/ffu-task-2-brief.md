### Task 2: FU2 Path harden

**Files:** `FwMapVersion.cs` (+ LayerCode), migration, `FourWayPathDispatcher.cs`, `FourWayWcsPack.CancelLeg`, `FourWaySchedulerHostedService` or Path retry helper, tests

- [x] Add `Fw_MapVersion.LayerCode`; ResolveMap: layer → active → first
- [x] Retry grant for Routing tasks stuck without grant (scheduler tick)
- [x] Cancel/Fail release all edges for shuttle ownerId
- [x] Tests: retry after grant free; cancel clears flow; two layers two maps
- [x] Report `.superpowers/sdd/ffu-task-2-report.md`

---
