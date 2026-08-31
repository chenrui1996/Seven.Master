# Task I-3 Review: Reset API

**Reviewer:** Code review subagent  
**Date:** 2026-08-30  
**Scope:** Spec compliance + code quality (read-only; brief, report, diff, source verified)

---

## Verdicts

| Dimension | Result |
|-----------|--------|
| **Spec compliance** | ✅ |
| **Task quality** | **Approved** |

---

## Spec Compliance Checklist

| Requirement | Status | Notes |
|-------------|--------|-------|
| `ResetAsync(projectName, ct)` on service interface + implementation | ✅ | `ISimulationDeployService` + `SimulationDeployService.ResetAsync` |
| Cancel/close unfinished `Bus_TransportOrder` for `SIM_{project}` warehouse | ✅ | Open statuses (`Created`–`Cancelling`) → `Failed`; legs → `Cancelled` |
| Cancel/close unfinished pack tasks for that warehouse | ✅ | Stacker + FourWay task tables; match by `LegId` or location codes |
| Do **not** delete `Wms_Location` | ✅ | No location mutations; test asserts count unchanged |
| `POST /api/simulation/reset` body `{ projectName }` | ✅ | `[Route("api/simulation")]` + `[HttpPost("reset")]` + `ResetRequest` |
| Test: after Deploy, Reset does not throw; deployment status unchanged; locations remain | ✅ | `Reset_AfterDeploy_ShouldKeepDeploymentAndLocations` |
| Test: open bus/stacker tasks cancelled | ✅ | `Reset_AfterDeploy_ShouldCancelOpenBusOrdersAndStackerTasks` (beyond minimum brief) |
| No git commit | ✅ | Report + working tree only |
| Look up Bus status enums; use terminal cancelled/failed states | ✅ | Aligns with `OrchestrationBus` patterns (`Failed` / `Cancelled`) |

### Behaviour verified

| Concern | Implementation |
|---------|----------------|
| Warehouse scope | Resolves `SIM_{SanitizeCode(projectName)}`; throws if missing |
| Location linkage | Loads warehouse `WmsLocation` codes; `Touches()` exact match (case-insensitive) |
| Deployment preserved | `SimDeployment.Status` untouched (tested) |
| External orders safe | Orders/tasks outside location set skipped |

---

## Findings

### Critical

*None.*

### Important

*None.*

### Minor

1. **Global in-memory scan** — `ResetAsync` loads all open bus orders (and each pack task type) then filters in memory. Acceptable for simulation scale; consider DB-side filters if production reuse is planned.

2. **Bus order match uses order-level From/To only** — Leg `FromCode`/`ToCode` are not used to select orders (only to propagate `affectedLegIds` after order match). Tasks can still be cancelled via location/`LegId` fallback. Unlikely edge case if order header locations differ from leg codes.

3. **Redundant `SaveChangesAsync` on empty location set** — Early return when warehouse has zero locations still calls save; harmless no-op.

4. **No BoxSort task cancellation** — Brief says “pack tasks”; domain has `BoxSort` pack id but no BoxSort task entities yet. Stacker + FourWay coverage matches current WCS surface.

5. **Diff artifact includes I-2 hunks** — `task-I-3-diff.txt` mixes Scada/Promote DTO and Deploy seed changes from I-2. I-3 deliverables themselves are isolated and correct; use report “Changes” section for I-3 scope.

6. **No WebApi integration test** — Controller follows `Undeploy` try/catch pattern; brief only required service tests.

---

## Code Quality Notes

**Strengths**

- Clear separation from `UndeployAsync`: runtime cleanup only, no deployment or location lifecycle changes.
- Consistent error messages and validation with existing deploy service methods.
- Dual linkage strategy (`affectedLegIds` + `Touches`) matches brief guidance for FK/location-based cancellation.
- Terminal states chosen appropriately per entity (`StkDeviceTask` → `Failed` where no `Cancelled` enum exists).
- TDD evidence: RED (`ResetAsync` missing) → GREEN (8/8 pass); two focused Reset facts with strong assertions.
- Controller endpoint mirrors existing simulation API conventions.

**Constraints verified**

- I-3 files: `ISimulationDeployService.cs`, `SimulationDeployService.cs`, `SimulationController.cs`, `SimulationDeployServiceTests.cs`.
- Local re-run: `dotnet test --filter FullyQualifiedName~SimulationDeploy` → **8 passed, 0 failed**.
- No LES references in Reset path; Promote API correctly out of scope.

---

## Recommendation

**Approve Task I-3.** Implementation satisfies all brief checkboxes; tests pass; Reset preserves deployment and locations while clearing open bus and pack runtime state.

**Suggested follow-ups (later tasks, optional):**

- WebApi smoke test for `POST /api/simulation/reset`.
- BoxSort task cancellation when entities exist.
- DB-filtered queries if Reset is used outside sim/dev volumes.
