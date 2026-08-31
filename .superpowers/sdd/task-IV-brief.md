# Task IV-1 + IV-2 + IV-3 (batch): 导入与运维增强

## IV-1 simproj adapter
- Create `Seven.Simulator/src/lib/project/simproj-adapter.ts`
- Map RCS-like fields: meta.name, map.devices, map.connections → sevenproj
- Unsupported device types → warnings[]
- Fixture: create `Seven.Simulator/fixtures/minimal.simproj.json` (small handcrafted sample, not full RCS demo)
- MapEditor or Features: import .simproj.json button using adapter
- Unit test / node script for adapter

## IV-2 Excel import (minimal)
- Backend: `POST /api/simulation/import-grid` accepting JSON body `{ packId, cells: [{ code, x, y }] }` that returns a SimMapDto-shaped payload OR accept multipart later
- Prefer **JSON grid import** if Excel libs heavy — document as Phase IV subset: "Excel → export CSV/JSON then import"; OR use a simple CSV upload
- Frontend: file input for JSON grid / simproj

## IV-3 route-groups preview (minimal)
- `POST /api/simulation/route-groups/preview` body: project map edges → returns `{ suggested: [{ code, nodeIds[] }], warnings: [] }` heuristic (connected components or cycles stub)
- Player sidebar or MapEditor panel shows preview result

## Docs
- Update doc/21 Phase IV section to match what was actually implemented
- No git commit
- Backend tests for preview + adapter; npm build green

Work root: d:\Junheinrich\Junheinrich.Master\Seven.Master
