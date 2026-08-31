# Task I-4 Review: Promote / promote-preview API

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
| Request `{ projectName, devices: [{ code, host, port, protocol }] }` | ✅ | `SimPromoteRequest` + `SimPromoteDeviceDto` in `Application/Simulator` |
| Separate `promote-preview` / `promote` endpoints (alt. to `apply?`) | ✅ | `POST /api/simulation/promote-preview`, `POST /api/simulation/promote` |
| Reject `127.0.0.1` / `localhost` / `::1` (case-insensitive) | ✅ | `IsLoopbackHost` + `ValidatePromoteDevices`; Theory covers preview (incl. `LOCALHOST`) and apply |
| Preview: return device list + warnings | ✅ | `SimPromotePreviewResult`; warns on missing / already-Promoted deployment |
| Apply: upsert `CommConnection` (DeviceComm entity exists) | ✅ | `UpsertCommConnectionsAsync` by `device.Code` → `Name` |
| Apply: persist promote snapshot on `Sim_Deployment` + `Status = Promoted` | ✅ | `ProjectJson` updated (`Promote`, `RuntimeMode = Production`); `deployment.Status = "Promoted"` |
| Apply: do **not** rename `SIM_` warehouse | ✅ | `warehouseCode` read from deployment only; test asserts `SIM_PROMOTEDEMO` unchanged |
| DTOs in `Application/Simulator` | ✅ | `SimPromoteRequest`, preview/result records, `SimPromoteDto` on `SimProjectDto` |
| Test: reject loopback | ✅ | `PromotePreview_ShouldRejectLoopbackHost`, `Promote_ShouldRejectLoopbackHost` |
| Test: preview returns devices | ✅ | `PromotePreview_ShouldReturnDevicesAndWarnings` |
| Test: apply marks deployment Promoted | ✅ | `Promote_ShouldMarkDeploymentPromoted_AndUpsertCommConnection` (+ CommConnection upsert) |
| No git commit | ✅ | Report + working tree only |

### Behaviour verified

| Concern | Implementation |
|---------|----------------|
| Loopback guard | Shared `ValidatePromoteDevices` on preview and apply |
| Missing deployment | Preview returns warning; apply throws `InvalidOperationException` |
| Re-promote | Preview warns; apply allowed (overwrites CommConnection + snapshot) |
| Warehouse scope | Uses stored `deployment.WarehouseCode`; no `WmsWarehouses` rename |
| CommConnection fields | Host, Port, Protocol, Enabled, AutoConnect, Remark `"Simulator Promote"` |

---

## Findings

### Critical

*None.*

### Important

*None.*

### Minor

1. **No test for preview warning when deployment missing** — `GetPromoteWarningsAsync` adds `"未找到已部署工程"` but only the happy-path preview is asserted. Behaviour is correct; coverage gap only.

2. **No test for `ProjectJson` promote snapshot** — Apply sets `Meta.RuntimeMode = "Production"` and `Promote` section; not asserted in tests (CommConnection + status are).

3. **`Promote_ShouldRejectLoopbackHost` omits `LOCALHOST` case** — Preview Theory includes it; apply Theory has three hosts only. Low risk given shared validator.

4. **Empty / whitespace host not rejected** — `IsLoopbackHost` returns false for blank host; brief only lists loopback hosts. Optional hardening for production promote.

5. **Diff artifact mixes prior task hunks** — `task-I-4-diff.txt` includes I-2 DTO/Scada/Deploy seed and I-3 `Reset` endpoint. I-4 deliverables are identifiable via report and Promote grep; use report “Changes” for I-4 scope.

6. **No WebApi integration test** — Controller follows existing try/catch + `WebResponseContent` pattern; brief only required service tests.

7. **`SimDeployment` has no `Remark` column** — Brief wording “remark/status Promoted” satisfied via `Status`, `ProjectJson` snapshot, and `CommConnection.Remark`; entity model has no separate remark field.

---

## Code Quality Notes

**Strengths**

- Clean split: preview is read-only validation + warnings; apply enforces deployment existence then mutates.
- Loopback rejection centralized in `ValidatePromoteDevices`, reused by both entry points.
- CommConnection upsert is straightforward (lookup by `Name`, update or insert) and matches brief “upsert by name/code”.
- Promote does not touch warehouse lifecycle — consistent with sim-to-production handoff semantics.
- TDD evidence in report: RED (missing methods) → GREEN; local re-run confirms **9 Promote / 17 SimulationDeploy** tests pass.
- Controller endpoints mirror `Deploy` / `Undeploy` conventions under `api/simulation`.

**Constraints verified**

- I-4 files: `ISimulationDeployService.cs`, `SimulationDeployService.cs`, `SimulationController.cs`, `SimulationDeployServiceTests.cs`.
- Simulator UI (I-5) correctly out of scope.

---

## Recommendation

**Approve Task I-4.** Implementation satisfies all brief checkboxes; tests pass; Promote preserves `SIM_` warehouse while upserting CommConnection and marking deployment Promoted.

**Suggested follow-ups (later tasks, optional):**

- Assert preview warning when project not deployed.
- Assert deserialized `ProjectJson` after promote (`RuntimeMode`, `Promote.devices`).
- WebApi smoke test for `POST /api/simulation/promote-preview` and `promote`.
