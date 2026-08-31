# Task 7 Report: Ifc_ApiLog + Ctl_Mode 联锁

**Branch:** `feature/wms-wcs-pack`  
**Date:** 2026-08-29  
**Status:** DONE  
**Commits:** none (per user rule)

---

## Summary

Implemented platform interface logging and control-mode interlock infrastructure.

- **`Ifc_ApiLog`** (`Ifc_` prefix): direction, system code, correlation/Leg/Order, path, bodies, duration, success
- **`Ctl_Mode`** (`Ctl_` prefix): scope (`Global` or PackId), mode (Auto/Semi/Manual), EStop, UpdatedAt
- **`IInterfaceLogService.WriteAsync` / `CountAsync`** — persists audit rows
- **`IControlModeService`** — Get/SetMode/SetEStop; `CanAcceptLegsAsync` merges Global EStop + pack EStop/Manual
- **`StackerWcsPack`** — `AcceptLegAsync` and `HealthAsync` reject when interlock blocks (EStop or Manual)
- Thin auth-gated APIs: `api/ControlMode/{scope}`, `api/InterfaceLog/count`
- **`AddSevenPlatform()`** always registered (not feature-gated)
- EF migration **`AddPlatformIfcCtl`** with Global Auto/EStop=false seed

**Did not:** SCADA (Task 10), HttpClient logging middleware, Vue UI, commit.

---

## TDD Evidence

### Tests — `PlatformIfcCtlTests` (4)

```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~PlatformIfcCtlTests"
```

**Outcome:** Exit code **0** — 4 passed

| Test | Asserts |
|------|---------|
| `WriteLog_ThenCount_ShouldPersistEntry` | Write → Count=1, row fields |
| `EStopTrue_StackerAcceptLeg_ShouldReject` | Accepted=false, no Stk task |
| `ManualMode_StackerAcceptLeg_ShouldReject` | Accepted=false |
| `GlobalEStop_StackerHealth_ShouldNotAcceptLegs` | CanAcceptLegs=false |

### Regression

```powershell
dotnet test ... --filter "FullyQualifiedName~StackerPackTests|FullyQualifiedName~OrchestrationBusTests"
```

**Outcome:** 6 passed, 0 failed

---

## Files Changed

| Action | Path |
|--------|------|
| Create | `Seven.Domain/Enums/PlatformEnums.cs` |
| Create | `Seven.Domain/Entities/Platform/IfcApiLog.cs` |
| Create | `Seven.Domain/Entities/Platform/CtlMode.cs` |
| Create | `Seven.Application/Platform/PlatformModels.cs` |
| Create | `Seven.Application/Platform/IPlatformServices.cs` |
| Create | `Seven.Infrastructure/Persistence/Configurations/Platform/PlatformConfigurations.cs` |
| Create | `Seven.Infrastructure/Platform/PlatformServices.cs` |
| Create | `Seven.Infrastructure/Platform/PlatformServiceCollectionExtensions.cs` |
| Create | `Seven.WebApi/Controllers/Platform/PlatformControllers.cs` |
| Create | `Seven.Tests/Platform/PlatformIfcCtlTests.cs` |
| Modify | `Seven.Infrastructure/Persistence/SevenDbContext.cs` |
| Modify | `Seven.Infrastructure/DependencyInjection.cs` |
| Modify | `Seven.Infrastructure/Wcs/Packs/Stacker/StackerWcsPack.cs` |
| Modify | `Seven.Tests/Wcs/StackerPackTests.cs` |
| Create | `Seven.Infrastructure/Migrations/20260829045151_AddPlatformIfcCtl.cs` (+ Designer, Snapshot) |

---

## Interlock Rules

1. **Global EStop** → all packs blocked
2. **Pack EStop** → that pack blocked
3. **Manual mode** — pack row if persisted, else Global mode; Manual blocks AcceptLeg
4. Semi/Auto allow acceptance when EStop clear

---

## Concerns / Follow-ups

1. **No HttpClient/middleware logging** — `IInterfaceLogService` ready; wire in Task 9 external WCS
2. **No Vue admin page** — API only (Task 7 Step 3 deferred)
3. **OrchestrationBus** does not yet surface interlock reject reason to caller — pack returns `AcceptLegResult(false, reason)` but bus may need explicit handling
4. **Semi mode** accepts legs — business rules for semi-auto dispatch not defined
5. **Global Manual without pack row** blocks all packs inheriting Global mode

---

## Out of Scope (confirmed not done)

- SCADA / `Scd_*` tables
- Git commit
