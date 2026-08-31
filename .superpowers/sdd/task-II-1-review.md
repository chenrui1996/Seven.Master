# Task II-1 Review: Canvas 渲染内核

**Reviewer:** Code review subagent  
**Date:** 2026-08-30  
**Scope:** `task-II-1-brief.md` 对照 `Seven.Simulator/src/components/map/*` + `MapEditor.vue` 集成；复验 build / test:hitTest

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
| `Seven.Simulator/src/components/map/MapCanvas.vue` | ✅ | Canvas 绘制、DPR、ResizeObserver、交互完整 |
| `Seven.Simulator/src/components/map/useMapCamera.ts` | ✅ | offset/scale、screen↔world、指针中心缩放 |
| `Seven.Simulator/src/components/map/hitTest.ts` | ✅ | 72×32 锚点、中心点/边界、top-most 命中 |
| `Seven.Simulator/src/components/map/hitTest.test.ts` | ✅ | 8 条断言，含重叠节点 z-order |
| `Seven.Simulator/src/views/MapEditor.vue` | ✅ | 工具栏下、表单上集成；选中回填；清空清选中 |

---

## Spec compliance checklist（brief）

| Requirement | Status | Evidence |
|-------------|--------|----------|
| 创建 `MapCanvas.vue` | ✅ | 198 行，`<script setup>` + canvas 模板 |
| 创建 `useMapCamera.ts` | ✅ | pan / zoomAt / screenToWorld / worldToScreen |
| 创建 `hitTest.ts` | ✅ | `NODE_WIDTH/HEIGHT`、`nodeCenter`、`hitTestNode` |
| 可选 hitTest 纯函数测试 | ✅ | `npm run test:hitTest` → 8 assertions passed |
| Canvas 绘制 nodes（方/圆角 + code）与 edges（中心连线） | ✅ | `draw()` 圆角矩形 + `nodeCenter` 连线 |
| 平移（拖拽）+ 缩放（滚轮） | ✅ | mousedown/move/up + wheel；0.25×–4× |
| 点击选中节点，`emit('select', id)` | ✅ | `onPointerUp` → `hitTestNode` → emit |
| Props: `nodes`, `edges`, `selectedId` | ✅ | `defineProps` 与 MapEditor 绑定一致 |
| MapEditor 集成（表单上方保留 Phase I 编辑） | ✅ | `:nodes/:edges/:selected-id` + `@select` |
| 无 three.js | ✅ | map 目录与 package.json 均无 three |
| 无 git commit | ✅ | 按 brief 要求（未审查 commit 历史） |
| Build green | ✅ | 复验 `npm run build` exit 0 |

---

## Implementation quality

### Strengths

1. **职责分离清晰** — 相机（`useMapCamera`）、命中（`hitTest`）、渲染（`MapCanvas`）三层拆分，便于 II-2 拖放/连线扩展。
2. **绘制与命中一致** — `NODE_WIDTH` / `NODE_HEIGHT` 共享常量，draw 与 hitTest 同一锚点语义（top-left）。
3. **生产级 Canvas 细节** — devicePixelRatio、`ResizeObserver`、以指针为中心的 zoom 数学、选中高亮、空态提示。
4. **测试覆盖合理** — 边界、空列表、重叠 top-most 四类场景；tsx 一次性脚本符合 brief「vitest 或 node 风格」。

### MapEditor 集成

- `onSelectNode` 将选中 id 回填 `code/x/y` 工具栏字段。
- `clearMap` 同步 `selectedNodeId = null`。
- MapCanvas 置于 toolbar 与边/申请点卡片之间，符合「表单上方」布局意图。

---

## Findings

### Critical

*None.*

### Minor（非阻塞）

1. **平移语义略宽于 brief** — brief 写「drag background」；实现为任意拖拽（含节点上）均平移。Phase II 可接受，后续若需「拖节点改坐标」需区分 hit layer。
2. **主计划 Step 2 部分未闭环** — `2026-08-29-seven-simulator.md` Step 2 含「属性面板改 code/x/y」；当前仅选中→回填表单，`addNode` 仍总是新增。brief 未强制双向编辑，留待 II-2 或小幅 MapEditor 补丁。
3. **import 风格不一致** — `MapCanvas.vue` 用 `@/lib/project/schema`，同项目其余文件用相对路径；build 已通过（Vite 8 + tsconfig paths），建议后续统一。
4. **仅 MouseEvent** — 未用 Pointer Events；桌面编辑器足够，移动端/触控板高级手势未覆盖。
5. **hitTest 右/下边 inclusive** — `worldX <= b.right` 在像素边界可能多命中 1px；与 draw 视觉偏差极小。

---

## Verification（review 复验）

```
npm run build        → ✅ exit 0 (vue-tsc + vite build)
npm run test:hitTest → ✅ hitTest: 8 assertions passed
```

---

## Recommendation

**Approve Task II-1.** Brief 所列交付物与行为均已实现；架构干净、测试与构建通过。Spec ✅，Quality **Approved**。上述 Minor 项可在 II-2（拖放/属性双向绑定）一并处理。
