### Task 6: 堆垛机包 — 分配与任务（语义触发，无物理通讯）

**PackId:** `"stacker"`

**Files under `Seven.Infrastructure/Wcs/Packs/Stacker/`:**
- `StackerWcsPack.cs` : `IWcsPack`
- `StackerDestinationService.cs` — subscribe `IEquipmentTriggerPort.DestinationRequested`
- `StackerAisleAllocator.cs` / `StackerLocationAllocator.cs`
- `StackerSchedulerHostedService.cs` (optional thin; can drive from destination service)
- Entities (`Stk_`): `StkRequestPoint`, `StkAssignmentPolicy`, `StkAssignmentRecord`, `StkPutAwayTask`, `StkDeviceTask`
- Configs + migration `AddStackerPack`
- Register when `Features.WcsPacks.Stacker` via `AddSevenWcs`

**Behavior:**
1. `AcceptLegAsync` → create `StkPutAwayTask` linked to LegId; status Accepted
2. On `DestinationRequested` (SUDR): resolve RequestPoint type:
   - AisleRequest → SelectAisle (filter by policy height/weight/availability; rotate AssignmentRecord) → set aisle on putaway → `DispatchDestinationAsync` to aisle EP/point
   - LocationRequest → SelectLocation in aisle → DispatchDestination to bin
3. On segment feedback complete (or simplify: after DispatchDestination, test can SimulateSegmentFeedback) → complete DeviceTask → eventually `IOrchestrationBus.OnLegEvent(Completed)`
4. `CanHandleAsync` → true for V1 if Stacker enabled (or always true when only pack)

**Hard rules:**
- NO use of Fw_ tables or FourWay types
- Allocators only use Stk_ policy tables + may read Wms_Location by Code for coordinates
- Use IEquipmentTriggerPort only (InMemory)

**Tests (`StackerPackTests`):**
1. SelectAisle rotates / respects unavailable aisle
2. SimulateDestinationRequest → DispatchDestination recorded on port
3. AcceptLeg + simulated complete path → OnLegEvent Completed (use real OrchestrationBus + InMemory port + Stacker pack in one test if feasible)

**不要：** real PLC, FourWay, commit, RGV complexity.

Seed minimal RequestPoint + AssignmentPolicy in test setup.
