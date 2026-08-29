# FFU Task 1 Report — FU1 Dual-pack Trigger NG silence

**Status:** Done  
**Date:** 2026-08-29  
**Branch:** feature/wms-wcs-pack (no commit)

## Changes

1. **`StackerDestinationService`**: On `CheckResult≠OK`, if enabled `FwRequestPoint` matches `SourcePointCode` → silent return (no Reject / no Failed).
2. **`FourWayDestinationService`**: Symmetric for enabled `StkRequestPoint`.
3. **`InboundToFourWayE2ETests`**: Removed dead `WcsPackResolver` line in `DualPack_FourWay_ShouldNotAccept_PureStackerLeg`; added dual-subscribe NG tests.
4. **`InMemoryEquipmentTriggerPort`**: Await all multicast handlers via `GetInvocationList()` so dual Dest subscriptions complete before assertions.

## Tests

| Test | Result |
|------|--------|
| `DualPack_FwRequestPoint_CheckNg_ShouldOnlyFourWayReject` | Pass |
| `DualPack_StkRequestPoint_CheckNg_ShouldOnlyStackerReject` | Pass |
| `DualPack_FourWay_ShouldNotAccept_PureStackerLeg` | Pass |
| `InboundToFourWayE2E` happy path | Pass |
| `StackerPackTests.SimulateDestinationRequest_CheckNg_ShouldReject` | Pass |
| `FourWayPackTests.SimulateDestinationRequest_CheckNg_ShouldReject` | Pass |

Filter: `InboundToFourWayE2ETests|StackerPackTests.SimulateDestinationRequest_CheckNg|FourWayPackTests.SimulateDestinationRequest_CheckNg` → **6/6 pass**.

## Concerns

- Production HostedService trigger wiring may still use multicast `event Func<...,Task>` without awaiting all handlers; in-memory port is fixed for tests. Worth aligning hosted path if dual-pack is live-subscribed the same way.
- Unknown (non-Fw / non-Stk) NG still Rejects per pack (Stacker may Reject; FourWay may Reject) — intentional per spec.
