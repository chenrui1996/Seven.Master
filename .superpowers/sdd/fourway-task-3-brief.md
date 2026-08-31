### Task 3: PutAway + DestinationService + CanHandle (F3)

**Files:** `FourWayWcsPack.cs`, `FourWayDestinationService.cs`, `FourWaySchedulerHostedService.cs`, DI

**Consumes:** Task 1鈥?  
**Produces:** SUDR aisle/location assign + segment complete 鈫?Bus

- [ ] **Step 1:** `FourWayWcsPack.CanHandleAsync`: true only if both ends start with `Fw.` OR either end is a known handover (if `Wms_HandoverLink` exists for the pair); else false. `AcceptLegAsync`: create `FwPutAwayTask` (inbound RefType) + keep/create `FwShuttleTask` as device carrier **or** defer ShuttleTask until F4 鈥?**Sprint 1 choice:** PutAway is source of truth; create lightweight `FwShuttleTask` Accepted for Leg compatibility with existing pack.

- [ ] **Step 2:** Implement `FourWayDestinationService` mirroring Stacker S1 subset:
  - CheckResult鈮燨K 鈫?Reject + PutAway Failed
  - LayerRequest / AisleRequest 鈫?allocate layer/aisle (or read Assigned*) 鈫?DispatchDestination(Ep)
  - LocationRequest 鈫?SelectLocation+Book 鈫?Dispatch(Bin)
  - SegmentFeedback 鈫?PutAway Completed 鈫?`_bus.OnLegEventAsync(Completed)`

- [ ] **Step 3:** Wire DI: register DestinationService; Scheduler HostedService creates scope, resolves DestinationService.Subscribe() (same pattern as Stacker).

- [ ] **Step 4:** Unit test: NG reject; AisleRequest dispatches EpPoint

---


