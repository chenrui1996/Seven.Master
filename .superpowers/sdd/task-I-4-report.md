# Task I-4 Report: Promote / promote-preview API

**Status:** DONE  
**Date:** 2026-08-30  
**Commits:** none (per task constraint)

## Summary

Added `PromotePreviewAsync` / `PromoteAsync` with DTOs, loopback rejection, CommConnection upsert, and `Sim_Deployment` status `Promoted`. Exposed `POST /api/simulation/promote-preview` and `POST /api/simulation/promote`. All `SimulationDeployServiceTests` pass (17/17).

## TDD Cycle

### RED

Added 5 tests before implementation:

| Test | Expected failure |
|------|------------------|
| `PromotePreview_ShouldRejectLoopbackHost` (×4 hosts) | methods missing |
| `Promote_ShouldRejectLoopbackHost` (×3 hosts) | methods missing |
| `PromotePreview_ShouldReturnDevicesAndWarnings` | methods missing |
| `Promote_ShouldMarkDeploymentPromoted_AndUpsertCommConnection` | methods missing |

```
dotnet test --filter "FullyQualifiedName~Promote"
  CS0535: PromotePreviewAsync / PromoteAsync not implemented
```

### GREEN

Implemented service + controller; re-ran tests:

```
dotnet test --filter "FullyQualifiedName~SimulationDeploy"
  Passed: 17, Failed: 0
```

## Changes

### `ISimulationDeployService.cs`

- `SimPromoteRequest`, `SimPromotePreviewResult`, `SimPromoteResult`
- `PromotePreviewAsync`, `PromoteAsync`

### `SimulationDeployService.cs`

- Rejects `127.0.0.1` / `localhost` / `::1` (case-insensitive)
- Preview: returns device list + warnings (e.g. no deployment)
- Apply: upserts `CommConnection` by device `Code` → `Name`; sets deployment `Status = Promoted`; updates `ProjectJson` promote snapshot + `RuntimeMode = Production`; **does not** rename `SIM_` warehouse

### `SimulationController.cs`

- `POST /api/simulation/promote-preview` → `{ projectName, devices }`
- `POST /api/simulation/promote` → same body, applies changes

### `SimulationDeployServiceTests.cs`

- Added 5 Promote tests (7 cases incl. Theory rows)

## Out of Scope

- Simulator UI (I-5)
- Git commit
- Warehouse rename to production code

## Concerns

None.
