# Task IV Report — 导入与运维增强 (batch)

**Date:** 2026-08-30  
**Scope:** IV-1 simproj adapter · IV-2 JSON grid import · IV-3 route-groups preview  
**Git commit:** none (per brief)

## Summary

Phase IV MVP delivered: RCS-like `.simproj.json` → `.sevenproj` adapter (client), JSON grid import API, route-groups preview API + UI. Excel native upload deferred; doc notes CSV/JSON export path.

## IV-1 — simproj adapter

| Item | Status |
|------|--------|
| `Seven.Simulator/src/lib/project/simproj-adapter.ts` | ✅ |
| `fixtures/minimal.simproj.json` | ✅ |
| MapEditor「导入 .simproj.json」 | ✅ |
| Unit test `npm run test:simproj` | ✅ 3 cases |

Maps `meta.name`, `map.nodes/edges/devices/connections/requestPoints` → `mergeSimProject()`. Unsupported device types (e.g. Shuttle) → `warnings[]`, import not blocked.

## IV-2 — JSON grid import

| Item | Status |
|------|--------|
| `POST /api/simulation/import-grid` | ✅ |
| `ISimulationImportService` + `SimulationImportService` | ✅ |
| MapEditor「导入栅格 JSON」 | ✅ |
| `fixtures/sample-grid.json` | ✅ |

Body: `{ packId, cells: [{ code, x, y }] }` → `{ map: SimMapDto, warnings }`. Frontend merges nodes via `store.applyImportedMap()`.

**Not implemented:** multipart xlsx upload (Phase IV subset per brief).

## IV-3 — route-groups preview

| Item | Status |
|------|--------|
| `POST /api/simulation/route-groups/preview` | ✅ |
| Heuristic: connected components + cycle warning | ✅ |
| MapEditor toolbar + Player sidebar UI | ✅ |

Returns `{ suggested: [{ code, nodeIds[] }], warnings }`. Four-way pack uses `Fw.RG-NN` codes.

## Backend tests

```
dotnet test --filter FullyQualifiedName~SimulationImport
→ 4/4 passed
```

## Frontend

```
npm run test:simproj  → 3 cases passed
npm run build         → green
```

## Docs

- Updated `doc/21-仿真器与联调闭环.md` Phase IV section, API table §5, acceptance steps.

## Files touched (new/modified)

**New:** simproj-adapter.ts, simproj-adapter.test.ts, minimal.simproj.json, sample-grid.json, ISimulationImportService.cs, SimulationImportService.cs, SimulationImportServiceTests.cs  

**Modified:** SimulationController.cs, SimulatorServiceCollectionExtensions.cs, project.ts, MapEditor.vue, Player.vue, package.json, doc/21

## Manual smoke (optional)

1. MapEditor → 导入 `fixtures/minimal.simproj.json` → warning for Shuttle, Deploy if topology OK.
2. MapEditor → 导入 `fixtures/sample-grid.json` → nodes appear.
3. Player / MapEditor → 路径组预览 on a map with edges.
