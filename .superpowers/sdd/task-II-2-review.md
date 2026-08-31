# Task II-2 Review: 设备库与连线

**Reviewer:** Code review subagent  
**Date:** 2026-08-30  
**Scope:** `task-II-2-brief.md` 对照 `deviceCatalog.ts`、`deployCompile.ts`、`schema.ts`、`MapCanvas.vue`、`MapEditor.vue`、`hitTest.ts`；复验 build / test:hitTest

---

## Verdicts

| Dimension | Result |
|-----------|--------|
| **Spec compliance（brief）** | ✅ |
| **Task quality** | **Approved** |

---

## Files spot-checked

| File | Status | Notes |
|------|--------|-------|
| `Seven.Simulator/src/components/map/deviceCatalog.ts` | ✅ | 四类型 + 默认端口、形状/颜色、port 工具函数 |
| `Seven.Simulator/src/lib/project/deployCompile.ts` | ✅ | `compileMapForDeploy` 合成 edges / requestPoints，不污染设计态 |
| `Seven.Simulator/src/lib/project/schema.ts` | ✅ | `SimMapConnection`、`map.connections`、Deploy 规则 JSDoc |
| `Seven.Simulator/src/components/map/MapCanvas.vue` | ✅ | 设备/连线/节点分层绘制；放置模式；拖放 |
| `Seven.Simulator/src/views/MapEditor.vue` | ✅ | 设备库面板、连线表单、节点写回、Deploy 编译 |
| `Seven.Simulator/src/components/map/hitTest.ts` | ✅ | 新增 `hitTestDevice` + `deviceBounds` |

---

## Spec compliance checklist（brief）

| Requirement | Status | Evidence |
|-------------|--------|----------|
| 创建 `deviceCatalog.ts`（SRM / CoordPoint / Conveyor / RequestPoint + 默认端口） | ✅ | `DEVICE_CATALOG` 四条目，各带 `ports[]`；`nextDeviceCode` / `portWorldPosition` |
| Palette：拖拽或点击放置 → `map.devices` | ✅ | `startPlacement` + `@place-device`；`draggable` + `onDrop` / `application/x-seven-device` |
| Canvas 上设备与库位节点区分绘制 | ✅ | 节点蓝矩形；设备按 `shape`（rect/diamond/circle）+ 端口圆点；连线橙色虚线 |
| 连线 `from/to: "deviceId.port"` → `map.connections` | ✅ | `formatPortRef(d.id, p.id)`；`SimMapConnection` in schema |
| schema.ts 补充 connections（若缺失） | ✅ | `connections: SimMapConnection[]` + `defaultSimProject` / `mergeSimProject` |
| 修复 II-1 Minor：选中节点编辑 code/x/y 写回 `map.nodes` | ✅ | `syncSelectedNode()` + `watch([code, x, y], ...)` |
| Deploy 规则文档化（schema 注释） | ✅ | `SimMapConnection` 上方 Phase II 规则块 |
| RequestPoint 连线 → 合成 `requestPoints` | ✅ | `compileMapForDeploy` L78–90 |
| 其它设备连线 → 最近库位节点间 `edges` | ✅ | `nearestNode` + 去重 `edgeKey`；同节点跳过 |
| Deploy 前 client-side 编译（MapEditor POST 前） | ✅ | `deploy()` → `compileMapForDeploy(store.project.map)` |
| 无 git commit | ✅ | 按 brief 要求 |
| Build green | ✅ | 复验 `npm run build` exit 0 |

---

## Implementation quality

### Strengths

1. **catalog 与渲染/编译共用几何** — `catalogEntry`、`portWorldPosition`、`deviceCenter` 被 MapCanvas 与 `deployCompile` 复用，端口坐标一致。
2. **设计态与 Deploy 态分离** — `connections` 仅持久化于工程 JSON；Deploy 时 `compileMapForDeploy` 返回新 map 对象，不 mutate store。
3. **交互完整** — 点击/拖拽双放置路径；放置模式 crosshair + 边框高亮；设备命中优先于节点（`hitTestDevice` 先于 `hitTestNode`）。
4. **II-1 遗留项闭环** — 选中节点 ↔ 表单双向绑定，`onSelectNode` 回填 + `watch` 写回。
5. **清理逻辑** — `removeDevice` 级联删除相关 connections；`clearMap` 清空 devices/connections。

### MapEditor / MapCanvas 集成

- 设备库卡片位于 toolbar 与 MapCanvas 之间，连线/边/申请点三列卡片在 Canvas 下方，布局清晰。
- `portOptions` computed 从已放置设备生成端口下拉，连线表单 UX 可用。
- Deploy hint 文案指向 schema 注释，与 brief「documented rule」一致。

---

## Findings

### Critical

*None.*

### Minor（非阻塞）

1. **`deployCompile` 无单测** — 纯函数、规则明确（nearest-node 启发式），建议 Phase II 后续补 3–4 条断言（RP 连线、设备边合成、重复边跳过、手动 edges 保留）。
2. **`hitTestDevice` 未测** — `test:hitTest` 仍仅覆盖 node 8 条；设备矩形命中与 top-most 顺序未回归。
3. **RequestPoint ↔ RequestPoint 连线** — `involvesRequestPoint` 分支只合成一侧 RP；brief 允许「keep rule simple」，双 RP 场景极少见。
4. **设备选中无属性编辑** — 可选中设备但无 x/y/code 表单；brief 未要求，Phase III 可扩展。
5. **选中节点时「添加节点」仍用同一表单** — 改 x/y 会先写回选中节点再新增；II-1 既有行为，非 II-2 回归。
6. **import 路径风格** — MapCanvas / deviceCatalog 用 `@/` alias，MapEditor 用相对路径；build 已通过，与 II-1 review 一致。
7. **MapEditor.vue 空行偏多** — 每行间双换行，可读性略差，不影响功能。

---

## Verification（review 复验）

```
npm run build        → ✅ exit 0 (vue-tsc + vite build)
npm run test:hitTest → ✅ hitTest: 8 assertions passed
```

---

## Recommendation

**Approve Task II-2.** Brief 所列交付物与行为均已实现；设备库、连线、Deploy 编译与 II-1 节点写回修复均到位。Spec ✅，Quality **Approved**。Minor 项（deployCompile 单测、设备 hitTest 覆盖）可在 II-3 或联调阶段一并补强。
