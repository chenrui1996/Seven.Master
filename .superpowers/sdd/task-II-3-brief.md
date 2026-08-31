# Task II-3: Space 编译与拓扑校验

**Create:**
- `Seven.Simulator/src/lib/map/topologyValidator.ts` — validate nodes have unique codes; edges reference existing node ids; connections reference existing device ports; at least 1 node for deploy; packId set
- Optional thin `spaceCompiler.ts` — if devices lack space, no-op or stub positions from x,y (don't port full RCS compiler)

**MapEditor:**
- Show validation errors at top (el-alert list)
- Disable Deploy button when `topologyValidator` returns errors
- Re-validate on map changes

**Tests:** node script or vitest for topologyValidator cases (duplicate code, dangling edge).

No git commit. Build green.

Work root: d:\Junheinrich\Junheinrich.Master\Seven.Master
