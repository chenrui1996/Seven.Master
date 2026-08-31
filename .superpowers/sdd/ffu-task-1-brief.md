### Task 1: FU1 Dual-pack Trigger NG silence

**Files:** `StackerDestinationService.cs`, `FourWayDestinationService.cs`, DualPack / new tests

- [ ] Stacker: on CheckResult鈮燨K, if enabled `FwRequestPoint` matches SourcePointCode 鈫?return (no Reject)
- [ ] FourWay: symmetric for enabled `StkRequestPoint`
- [ ] Remove DualPack dead `WcsPackResolver` line
- [ ] Tests: Fw RP+NG 鈫?only FourWay Reject; Stk RP+NG 鈫?only Stacker Reject
- [ ] Report `.superpowers/sdd/ffu-task-1-report.md`

---


