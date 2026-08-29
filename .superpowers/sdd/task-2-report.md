# Task 2 Report: 包契约与通讯无关触发端口

**Branch:** `feature/wms-wcs-pack`  
**Date:** 2026-08-29  
**Status:** DONE  
**Commits:** none (per user rule)

---

## Summary

Implemented Phase A WCS application contracts and an in-memory equipment trigger port:

- Added `IWcsPack`, `IOrchestrationBus`, `IEquipmentTriggerPort` under `Seven.Application/Wcs/`
- Added `WcsModels.cs` with DTOs/records for legs, events, trigger commands, and health
- Implemented `InMemoryEquipmentTriggerPort` with simulate + dispatch recording
- Registered `IEquipmentTriggerPort` → `InMemoryEquipmentTriggerPort` as Singleton in `AddSevenWcs`
- Added TDD tests for destination request subscription and dispatch recording

No real `IOrchestrationBus` or `IWcsPack` implementations were added (Task 3+ scope).

---

## TDD Evidence

### RED — Step 1

**Action:** Created `Seven.Tests/Wcs/EquipmentTriggerPortTests.cs` with two tests from the brief.

**Command:**
```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~EquipmentTriggerPortTests" --no-restore
```

**Outcome:** Exit code **1** — compile errors (expected):
- `Seven.Application.Wcs` namespace not found
- `Seven.Infrastructure.Wcs.Triggers` namespace not found

### GREEN — Step 2

**Action:** Implemented contracts, models, in-memory port, and DI registration.

**Command:**
```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~EquipmentTriggerPortTests"
```

**Outcome:** Exit code **0**
```
已通过! - 失败: 0，通过: 2，已跳过: 0，总计: 2
```

**Regression (Task 1):**
```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~FeatureOptionsWcsTests|FullyQualifiedName~EquipmentTriggerPortTests"
```
```
已通过! - 失败: 0，通过: 4，已跳过: 0，总计: 4
```

---

## Files Changed

| Action | Path |
|--------|------|
| Create | `Seven.Net8/Seven.Application/Wcs/IWcsPack.cs` |
| Create | `Seven.Net8/Seven.Application/Wcs/IOrchestrationBus.cs` |
| Create | `Seven.Net8/Seven.Application/Wcs/IEquipmentTriggerPort.cs` |
| Create | `Seven.Net8/Seven.Application/Wcs/WcsModels.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Wcs/Triggers/InMemoryEquipmentTriggerPort.cs` |
| Modify | `Seven.Net8/Seven.Infrastructure/Wcs/WcsServiceCollectionExtensions.cs` |
| Create | `Seven.Net8/Seven.Tests/Wcs/EquipmentTriggerPortTests.cs` |

---

## Implementation Notes

### Application contracts

Interfaces match the brief verbatim. `WcsModels.cs` uses `record` types for immutability and value equality (supports FluentAssertions `Contain` on dispatch lists).

**`TransportLegDto`:** `LegId`, `OrderId`, `PackId`, `Seq`, `FromCode`, `ToCode`, `ContainerCode`, `HandoverIn`, `HandoverOut`.

**`DestinationRequestTrigger`:** positional record `(ContainerCode, SourcePointCode, Height, Weight, CheckResult)` — matches test constructor call.

**`LegEvent`:** `LegId`, `LegEventType` enum (`Progress`, `Completed`, `Failed`, `Cancelled`, `DestinationRequest`), optional `Status`/`Message`.

**Command/feedback records:** minimal fields for Phase A; `LegId` optional on dispatch/feedback for future correlation.

### InMemoryEquipmentTriggerPort

- Custom event add/remove with lock for thread-safe subscriber registration
- `SimulateDestinationRequestAsync` / `SimulateSegmentFeedbackAsync` invoke subscribers outside lock (avoid deadlock)
- `DispatchedDestinations` / `DispatchedMoves` lists guarded on write; sufficient for unit tests
- Implements `IEquipmentTriggerPort` only; simulate methods are port-specific test helpers

### DI

`AddSevenWcs` now registers:
```csharp
services.AddSingleton<IEquipmentTriggerPort, InMemoryEquipmentTriggerPort>();
```
Always-on for Phase A (real comm packs replace in Phase H per design).

---

## Self-Review

| Check | Result |
|-------|--------|
| Interfaces match brief exactly | OK |
| No OrchestrationBus / real pack impl | OK |
| TDD RED→GREEN documented | OK |
| Tests pass (2 new + 2 Task 1) | OK |
| DI Singleton registration | OK |
| Scope limited to Task 2 | OK |

**Minor notes (non-blocking):**
- No DI resolution test for `IEquipmentTriggerPort` — brief only required port unit tests; can add in Task 3 if needed
- `DispatchedDestinations`/`DispatchedMoves` exposed as mutable `List<>` — acceptable for test/debug port; Phase H may use internal storage
- `LegEventType`/`PackHealthDto` field shapes are minimal placeholders until bus/pack tasks flesh out status vocabulary

---

## Concerns / Follow-ups (Task 3+)

1. **Bus implementation** — `IOrchestrationBus` is contract-only; Task 4+ will need leg planning and `OnLegEventAsync` routing
2. **Production trigger port** — `InMemoryEquipmentTriggerPort` is default; Socket/Modbus/S7 packs should register conditionally in Phase H
3. **Model evolution** — dispatch/feedback records may gain device codes, priority, or vendor extensions when Stacker pack lands (Task 6)

---

## Out of Scope (confirmed not done)

- `IOrchestrationBus` implementation
- `IWcsPack` implementations (Stacker/FourWay/BoxSort)
- WMS entities / stock services (Task 3)
- Git commit
