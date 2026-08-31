# Task 10 Report: 2D SCADA 骨架

**Branch:** `feature/wms-wcs-pack`  
**Date:** 2026-08-29  
**Status:** DONE  
**Commits:** none (per user rule)

---

## Summary

Implemented 2D SCADA skeleton with view/bind entities, CRUD API, status polling, Vue read-only floor plan, and InMemory tests.

- **`Scd_View`** — Code, Name, Width, Height
- **`Scd_NodeBind`** — ViewId, LocationCode, X, Y, Label? (LocationCode 约定关联 Wms_Location，无 FK)
- **API** — `api/ScdView` / `api/ScdNodeBind` CRUD; `GET api/ScdView/{id}/status` JOIN `Wms_Location.IsOccupied`
- **Vue** — `Seven.Vue3/src/views/Scada/Floor2d.vue` 只读画布，5s 轮询
- **Feature gate** — `[RequiresFeature("Wms")]`; `ScadaFolder: 'wms'` in `features.ts`
- **Migration** — `AddScada2d` (`Scd_View`, `Scd_NodeBind`)

**Did not:** Three.js, SignalR, Sys_Menu seed, commit.

---

## TDD Evidence

```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~Scada2dTests"
```

**Outcome:** Exit code **0** — 3 passed

| Test | Asserts |
|------|---------|
| `CreateViewAndBind_ListByViewId_ShouldReturnCoordinates` | Add view+bind → X/Y returned |
| `GetStatus_ShouldJoinWmsLocationIsOccupied` | IsOccupied true/false from WmsLocation |
| `GetStatus_UnknownLocationCode_ShouldDefaultNotOccupied` | Missing location → false |

### Regression

```powershell
dotnet test ... --filter "FullyQualifiedName~PlatformIfcCtlTests|FullyQualifiedName~Scada2dTests"
```

**Outcome:** 7 passed, 0 failed

---

## Files Changed

| Action | Path |
|--------|------|
| Create | `Seven.Domain/Entities/Platform/ScdView.cs` |
| Create | `Seven.Domain/Entities/Platform/ScdNodeBind.cs` |
| Create | `Seven.Application/Scada/ScadaModels.cs` |
| Create | `Seven.Application/Scada/IScadaServices.cs` |
| Create | `Seven.Infrastructure/Persistence/Configurations/Platform/ScadaConfigurations.cs` |
| Create | `Seven.Infrastructure/Scada/ScadaViewService.cs` |
| Create | `Seven.Infrastructure/Scada/ScadaNodeBindService.cs` |
| Create | `Seven.Infrastructure/Scada/ScadaServiceCollectionExtensions.cs` |
| Create | `Seven.WebApi/Controllers/Scada/ScadaControllers.cs` |
| Create | `Seven.Tests/Scada/Scada2dTests.cs` |
| Create | `Seven.Vue3/src/views/Scada/Floor2d.vue` |
| Modify | `Seven.Infrastructure/Persistence/SevenDbContext.cs` |
| Modify | `Seven.Infrastructure/DependencyInjection.cs` |
| Modify | `Seven.Vue3/src/stores/features.ts` |
| Create | `Seven.Infrastructure/Migrations/20260829050144_AddScada2d.cs` (+ Designer, Snapshot) |

---

## Concerns / Follow-ups

1. **Sys_Menu seed** — `ScadaFolder` + `Floor2d` menu entries not seeded; requires manual menu config or future seed task.
2. **Permissions** — `ScdView.*` / `ScdNodeBind.*` permission keys referenced but not seeded in role auth.
3. **V2** — SignalR push for occupancy changes; Three.js optional 3D layer.
