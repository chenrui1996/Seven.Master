# WCS-S1 Stacker Destination Implementation Plan

> **For agentic workers:** Implement task-by-task; checkbox tracking.

**Goal:** Align Stacker SUDR reject + SelectAisle/SelectLocation core rules with LES semantics (no route/PLC).

**Architecture:** Enhance `StackerDestinationService` and allocators; add `RejectDestination` on trigger port; optional `MinEmptySlots`/`AllocationWeight` on policy.

**Tech Stack:** .NET 8, EF Core, xUnit/FluentAssertions

## Global Constraints

- Do not add Stk_Route / DeviceComm in this plan
- Keep existing StkRequestPointType numeric values (0/1/2)
- Prefix codes remain Stk.*; tests may use short aisle codes

---

### Task 1: Port reject + DestinationService

- [ ] Add `RejectDestinationCommand` + `RejectDestinationAsync` to port + InMemory list
- [ ] DestinationService: reject paths; BlockingPoint reassign
- [ ] Tests for NG check / missing task

### Task 2: Allocators

- [ ] Policy fields MinEmptySlots, AllocationWeight + EF migration
- [ ] Aisle: empty-slot filter, weight, Wms_Aisle.IsAvailable
- [ ] Location: depth-first order, set IsBooked
- [ ] Tests

### Task 3: Docs

- [ ] doc/21 + update doc/20, doc/README, srm-wcs/02 P0 ticks
