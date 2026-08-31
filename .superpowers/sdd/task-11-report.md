# Task 11 Report: 文档与演示开关

**Branch:** `feature/wms-wcs-pack`  
**Date:** 2026-08-29  
**Status:** DONE  
**Commits:** none (per user rule)

---

## Summary

Documentation and demo-switch verification for WMS/WCS pack scaffold (Tasks 1–10).

- **`doc/19-WMS与WCS包.md`** — ops guide: design links, table prefixes, feature matrix, InMemory TriggerPort note, Phase H deferred, checklist
- **`doc/README.md`** — index entry for doc 19
- **`doc/14-功能开关.md`** — Wms / OrchestrationBus / WcsPacks / ExternalWcs rows + recommended combos
- **`design/2026-08-29-wms-wcs-implementation-guide.md`** — §1.2 status note: Tasks 1–10 scaffolded
- **`HotStore:EnableDemoScheduler`** — verified **false** in `appsettings.json` and `appsettings.Testing.json` (unchanged)

**Did not:** rewrite architecture; commit.

---

## Verification

### EnableDemoScheduler

| File | Value |
|------|-------|
| `Seven.WebApi/appsettings.json` | `false` |
| `Seven.WebApi/appsettings.Testing.json` | `false` |

### Tests

```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~Wms|FullyQualifiedName~Wcs|FullyQualifiedName~Platform|FullyQualifiedName~FourWay|FullyQualifiedName~Stacker|FullyQualifiedName~Orchestration|FullyQualifiedName~Stock|FullyQualifiedName~Order|FullyQualifiedName~Scada|FullyQualifiedName~External"
```

**Outcome:** Exit code **0** — **29 passed**, 0 failed, 0 skipped

---

## Files Changed

| Action | Path |
|--------|------|
| Create | `doc/19-WMS与WCS包.md` |
| Modify | `doc/README.md` |
| Modify | `doc/14-功能开关.md` |
| Modify | `design/2026-08-29-wms-wcs-implementation-guide.md` |
| Create | `.superpowers/sdd/task-11-report.md` |

---

## Concerns / Follow-ups

1. **Sys_Menu seed** — WMS/WCS/SCADA menu entries still require manual config or future seed task (noted in Task 10).
2. **Phase H** — Socket/Modbus/S7 TriggerPort replacement documented as deferred; no runtime change in this task.
3. **appsettings.Development.json** — Wms/OrchestrationBus/WcsPacks default false; devs must enable per scenario (documented in doc 19).
