# Task I-2 Report: Deploy 增强 — 边 / 申请点 / Scd

**Status:** DONE  
**Date:** 2026-08-29  
**Commits:** none (per task constraint)

## Summary

Extended `SimulationDeployService` to materialize stacker routes/request points from map DTO collections, auto-provision default SCADA views when `scada.views` is absent, and retain/enhance four-way FwNode/FwRoute seeding. All `SimulationDeployServiceTests` pass (6/6).

## TDD Cycle

### RED

Added three new tests before implementation:

| Test | Expected failure |
|------|------------------|
| `Deploy_Stacker_WithEdgeAndRequestPoint_ShouldCreateRouteAndRequestPoint` | `EdgeCount` 0, no `StkRoutes` |
| `Deploy_ShouldAutoCreateScadaView_WhenViewsEmpty` | no `ScdViews` / `ScdNodeBinds` |
| `Deploy_FourWay_WithEdge_ShouldCreateFwNodeAndRoute` | already GREEN (existing four-way logic) |

```
dotnet test --filter "FullyQualifiedName~SimulationDeploy"
  Passed: 4, Failed: 2
```

### GREEN

Implemented deploy branches; re-ran tests:

```
dotnet test --filter "FullyQualifiedName~SimulationDeploy"
  Passed: 6, Failed: 0
```

## Changes

### `Seven.Infrastructure/Simulator/SimulationDeployService.cs`

1. **Stacker edges → `StkRoute`**
   - Iterates `project.Map.Edges ?? []`
   - Prefixes `From`/`To` via `PackCodeRules.EnsurePrefix`
   - Idempotent: skips duplicate `(MapCode="", FromCode, ToCode)`
   - Returns edge count for `DeployResult.EdgeCount`

2. **Stacker requestPoints → `StkRequestPoint`**
   - When `RequestPoints` non-empty: creates points from DTO (`Code`, `MappedLocationCode` → `AisleCode`)
   - When null/empty: **backward-compatible fallback** — seeds from nodes (existing behavior)

3. **Scada auto-provision**
   - When `Scada` is null or `Views` is null/empty:
     - Creates/reuses `ScdView` with `Code`/`Name` = `SIM_{SanitizeCode(projectName)}`
     - Canvas size from node bounds (min 800×600)
     - Binds each node as `ScdNodeBind` with prefixed `LocationCode`, `X`/`Y`, `Label`
   - When explicit views present: no-op (deferred to future task)

4. **Four-way**
   - Existing `EnsureFourWayMapAsync` already handles nodes→`FwNode`, edges→`FwRoute`; test added for regression coverage

### `Seven.Tests/Simulator/SimulationDeployServiceTests.cs`

Added 3 tests (see TDD section). Existing 3 tests unchanged and passing.

## Null-as-empty handling

| DTO field | Treatment |
|-----------|-----------|
| `Map.Edges` | `?? []` |
| `Map.RequestPoints` | `?? []` (empty → node fallback) |
| `Scada` / `Scada.Views` | null or empty → auto-create default view |

## Out of Scope

- Explicit `scada.views` deploy from DTO
- `Promote` API
- Four-way `FwRequestPoint` from DTO (only stacker request points in this task)
- Git commit

## Concerns

None. Location authority remains `Wms_Location`; SCADA binds reference location codes without FK.
