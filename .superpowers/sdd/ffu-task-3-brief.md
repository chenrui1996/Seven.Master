### Task 3: FU3 Parking / MaxShuttle

**Files:** `FourWayWcsPack` parking helpers, DestinationService Occupied transition, Allocator MaxShuttleCount, HostedService wake Suspended retrievals, tests

- [x] Atomic reserve (update where Free)
- [x] Reserved→Occupied on dispatch/segment start; Free on complete/cancel
- [x] Scheduler: wake parking-suspended Retrievals
- [x] Allocator: skip aisle when Reserved+Occupied ≥ MaxShuttleCount
- [x] Tests: concurrent reserve; wake after free; MaxShuttle skip
- [x] Report `.superpowers/sdd/ffu-task-3-report.md`

---
