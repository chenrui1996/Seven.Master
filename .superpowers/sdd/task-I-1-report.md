# Task I-1 Report: 扩展工程 schema 与 DTO

**Status:** DONE  
**Date:** 2026-08-29  
**Commits:** none (per task constraint)

## Summary

Extended the Seven.Simulator project schema (TypeScript) and backend DTOs (C#) to align with spec §3, covering `requestPoints`, `scada`, `promote`, and `meta.simCommsMode`. Backward-compatible JSON deserialization verified via new unit tests; existing deploy tests compile and pass unchanged.

## Changes

### 1. TypeScript — `Seven.Simulator/src/lib/project/schema.ts` (new)

- Defined `SimCommsMode`, `SimMapRequestPoint`, `SimScadaView`, `SimPromoteDevice`, and extended `SimProject` interface.
- Added `defaultSimFeatures()` and `defaultSimProject()` helpers with sensible defaults:
  - `meta.simCommsMode`: `"Trigger"`
  - `map.requestPoints`: `[]`
  - `scada.views`: `[]`
  - `promote.devices`: `[]`

### 2. TypeScript — `Seven.Simulator/src/stores/project.ts` (modified)

- Removed inline interface definitions; imports types and `defaultSimProject()` from `schema.ts`.
- Re-exports `SimFeatures` and `SimProject` for view components.
- Default project state now includes all new fields.

### 3. C# — `Seven.Application/Simulator/ISimulationDeployService.cs` (modified)

New records:

| Record | Fields |
|--------|--------|
| `SimMapRequestPointDto` | `Code`, `MappedLocationCode` |
| `SimScadaViewDto` | `Code`, `Name`, `Width`, `Height` |
| `SimScadaDto` | `Views` (optional, default null) |
| `SimPromoteDeviceDto` | `Code`, `Host`, `Port`, `Protocol` |
| `SimPromoteDto` | `Devices` (optional, default null) |

Extended records:

| Record | Addition |
|--------|----------|
| `SimMapDto` | `RequestPoints` optional param (default null) |
| `SimProjectMetaDto` | `SimCommsMode` optional param (default `"Trigger"`) |
| `SimProjectDto` | `Scada`, `Promote` optional params (default null) |

`SimFeaturesDto` unchanged — fully backward compatible.

### 4. Tests — `Seven.Tests/Simulator/SimProjectDtoTests.cs` (new)

- `Deserialize_MinimalJson_WithNodesOnly_Succeeds` — nodes-only JSON deserializes; `SimCommsMode` defaults to `"Trigger"`; new collections null when absent.
- `Deserialize_WithRequestPoints_HasNonEmptyRequestPoints` — full schema with `requestPoints`, `scada`, `promote`, `simCommsMode: Gateway`.

### 5. Existing tests — `SimulationDeployServiceTests.cs`

No changes required. Positional record constructors remain valid due to optional trailing parameters with defaults.

## Test Results

```
dotnet test --filter "FullyQualifiedName~SimProject"
  Passed: 2, Failed: 0

dotnet test --filter "FullyQualifiedName~SimulationDeploy"
  Passed: 3, Failed: 0
```

## Out of Scope (Task I-2+)

- Deploy logic for edges / requestPoints / scada.views
- three.js canvas
- `SIM_` warehouse prefix behavior changes
- Promote API implementation

## Concerns

None. Optional collections deserialize as `null` when absent (consistent with existing `Edges`/`Devices` pattern). Task I-2 Deploy service should treat null as empty list where needed.
