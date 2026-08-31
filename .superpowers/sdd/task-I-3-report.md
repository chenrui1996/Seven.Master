# Task I-3 Report: Reset API

**Status:** DONE  
**Date:** 2026-08-29  
**Commits:** none (per task constraint)

## Summary

Added `ResetAsync` to clear runtime state (open Bus transport orders and WCS pack tasks) for a `SIM_{project}` warehouse without touching deployment records or `Wms_Location` rows. Exposed `POST /api/simulation/reset` with body `{ projectName }`. All `SimulationDeployServiceTests` pass (8/8).

## TDD Cycle

### RED

Added two tests before implementation:

| Test | Expected failure |
|------|------------------|
| `Reset_AfterDeploy_ShouldKeepDeploymentAndLocations` | `ResetAsync` missing |
| `Reset_AfterDeploy_ShouldCancelOpenBusOrdersAndStackerTasks` | open orders/tasks not cancelled |

```
dotnet test --filter "FullyQualifiedName~SimulationDeploy"
  CS1061: ResetAsync not defined
```

### GREEN

Implemented `ResetAsync` + controller endpoint; re-ran tests:

```
dotnet test --filter "FullyQualifiedName~SimulationDeploy"
  Passed: 8, Failed: 0
```

## Changes

### `ISimulationDeployService.cs`

- Added `Task ResetAsync(string projectName, CancellationToken ct = default)`.

### `SimulationDeployService.cs`

- Resolves `SIM_{SanitizeCode(projectName)}` warehouse; throws if missing.
- Loads warehouse location codes; cancels open runtime entities touching those codes (or linked LegIds):
  - **Bus:** `BusTransportOrder` → `Failed`; non-terminal `BusTransportLeg` → `Cancelled`
  - **Stacker:** `StkPutAwayTask` / `StkRetrievalTask` → `Cancelled`; `StkDeviceTask` → `Failed`
  - **FourWay:** `FwShuttleTask`, `FwPutAwayTask`, `FwRetrievalTask`, `FwHoistTask`, `FwHoistExecTask` → `Cancelled`
- Does **not** change `SimDeployment.Status` or delete locations.

### `SimulationController.cs`

- `POST /api/simulation/reset` body `{ projectName }` → calls `ResetAsync`, returns `"已 Reset"`.
- Added `ResetRequest` record.

### `SimulationDeployServiceTests.cs`

- Added 2 Reset tests (see TDD section).

## Out of Scope

- Promote API
- Git commit
- LES references

## Concerns

None. Orders/tasks outside the SIM warehouse location set are left unchanged.
