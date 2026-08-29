# Task 1 Report: Features、表前缀常量与 DI 骨架

**Branch:** `feature/wms-wcs-pack`  
**Date:** 2026-08-29  
**Status:** DONE  
**Commits:** none (per user rule)

---

## Summary

Implemented Phase A scaffolding for WMS/WCS packs:

- Extended `FeatureOptions` with `Wms`, `OrchestrationBus`, nested `WcsPacks`, and `IsWcsPackEnabled(string)`
- Added `TablePrefixes` constants in `Seven.Domain.Wcs`
- Added `AddSevenWcs` DI shell and wired it in `AddSevenInfrastructure` after `AddSevenDeviceComm`
- Updated `appsettings.json` / `appsettings.Development.json` with default-false WCS feature flags
- Updated frontend `features.ts` with nested `wcsPacks` aligned to backend camelCase JSON

No `IWcsPack` or hosted services were added (Task 2+ scope).

---

## TDD Evidence

### RED — Step 1–2

**Action:** Created `Seven.Tests/Wcs/FeatureOptionsWcsTests.cs` with two tests from the brief.

**Command:**
```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~FeatureOptionsWcsTests" --no-restore
```

**Outcome:** Exit code **1** — compile errors (expected):
- `FeatureOptions` missing `Wms`, `OrchestrationBus`, `WcsPacks`
- Type `WcsPackFeatureOptions` not found
- Method `IsWcsPackEnabled` not found

### GREEN — Step 7

**Action:** Implemented all Task 1 production changes (see Files Changed below).

**Command:**
```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~FeatureOptionsWcsTests"
```

**Outcome:** Exit code **0**
```
已通过! - 失败: 0，通过: 2，已跳过: 0，总计: 2
```

---

## Files Changed

| Action | Path |
|--------|------|
| Modify | `Seven.Net8/Seven.Infrastructure/Configuration/AppOptions.cs` |
| Create | `Seven.Net8/Seven.Domain/Wcs/TablePrefixes.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Wcs/WcsServiceCollectionExtensions.cs` |
| Modify | `Seven.Net8/Seven.Infrastructure/DependencyInjection.cs` |
| Modify | `Seven.Net8/Seven.WebApi/appsettings.json` |
| Modify | `Seven.Net8/Seven.WebApi/appsettings.Development.json` |
| Modify | `Seven.Vue3/src/stores/features.ts` |
| Create | `Seven.Net8/Seven.Tests/Wcs/FeatureOptionsWcsTests.cs` |

---

## Implementation Notes

### FeatureOptions

Added verbatim from brief:
- `Wms`, `OrchestrationBus` bool properties
- `WcsPacks` nested object (`WcsPackFeatureOptions`: Stacker / FourWay / BoxSort)
- `IsWcsPackEnabled(string packName)` via reflection on `WcsPackFeatureOptions` (case-insensitive)

Existing `IsEnabled(string)` unchanged — only reads top-level bool properties; nested pack switches use `IsWcsPackEnabled`.

### TablePrefixes

Constants: `Wms_`, `Bus_`, `Stk_`, `Fw_`, `Ext_`, `Ifc_`, `Ctl_`, `Scd_`.

### AddSevenWcs

Minimal Phase A shell aligned with `AddSevenHotStore` / `AddSevenDeviceComm` pattern:
- Reads `Features` section from `IConfiguration`
- Does not register HostedService or `IWcsPack` (deferred to Task 2+)
- Called in `AddSevenInfrastructure` immediately after `AddSevenDeviceComm`

### appsettings

`Features` section extended with defaults `false`:
```json
"Wms": false,
"OrchestrationBus": false,
"WcsPacks": {
  "Stacker": false,
  "FourWay": false,
  "BoxSort": false
}
```

### Frontend features.ts

- Added `WcsPackFeatureFlags` interface and nested `wcsPacks` on `FeatureFlags`
- Defaults all false; `load()` deep-merges `wcsPacks` from API response
- Placeholder `menuFeatureMap` entry: `WmsFolder: 'wms'`

`GetFeatures` (`SevenControllers.cs`) already returns `IOptions<FeatureOptions>.Value` directly — new properties serialize as camelCase JSON without controller changes.

---

## Self-Review

| Check | Result |
|-------|--------|
| TDD RED then GREEN | ✓ |
| Exact code from brief for tests / TablePrefixes / FeatureOptions | ✓ |
| No Task 2+ (`IWcsPack`, HostedService) | ✓ |
| No git commit | ✓ |
| `IsEnabled` not used for nested packs | ✓ |
| DI call order: after DeviceComm | ✓ |
| Frontend nested JSON alignment | ✓ |

### Minor observations (non-blocking)

1. **`appsettings.Testing.json`** was not in the brief file list; it lacks explicit `Wms`/`WcsPacks` keys — defaults bind to `false` via type defaults. Can add in a later task if test env needs explicit keys.
2. **`isMenuEnabled`** does not yet map individual WCS pack menus (e.g. Stacker) — only `WmsFolder → wms` placeholder as specified; pack-level menu gating is future work.
3. Pre-existing build warnings in DeviceComm drivers / WorkFlowService unchanged.

---

## Test Summary

2/2 tests passed in `FeatureOptionsWcsTests` (nested property access + `IsWcsPackEnabled` resolution).
