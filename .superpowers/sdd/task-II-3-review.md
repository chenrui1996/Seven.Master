# Task II-3 Review: 拓扑校验与 Space 编译桩

**Reviewer:** Code review subagent  
**Date:** 2026-08-30  
**Scope:** `task-II-3-brief.md` 对照 `topologyValidator.ts`、`spaceCompiler.ts`、`topologyValidator.test.ts`、`MapEditor.vue`；复验 test:topology / test:hitTest / build

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
| `Seven.Simulator/src/lib/map/topologyValidator.ts` | ✅ | packId、节点数、唯一编码、边端点、连线端口五类规则 |
| `Seven.Simulator/src/lib/map/spaceCompiler.ts` | ✅ | 薄桩：`x/y` 默认值补全后透传 |
| `Seven.Simulator/src/lib/map/topologyValidator.test.ts` | ✅ | 6 用例覆盖 brief 要求的 duplicate / dangling |
| `Seven.Simulator/src/views/MapEditor.vue` | ✅ | `el-alert` 列表、`:disabled="!canDeploy"`、`deploy()` 二次守卫 |
| `Seven.Simulator/package.json` | ✅ | `test:topology` 脚本 |

---

## Spec compliance checklist（brief）

| Requirement | Status | Evidence |
|-------------|--------|----------|
| 创建 `topologyValidator.ts` — 节点 code 唯一 | ✅ | `seenCodes` + `DUPLICATE_NODE_CODE`；额外校验空 code |
| edges 引用现有 node id | ✅ | `nodeIds` Set；`EDGE_FROM_MISSING` / `EDGE_TO_MISSING` |
| connections 引用现有 device port | ✅ | `validatePortRef` → 格式 / 设备 / 类型 / 端口 |
| Deploy 前至少 1 个节点 | ✅ | `NO_NODES` |
| packId 已设置 | ✅ | `PACK_ID_MISSING`（含 `trim()`） |
| 可选 `spaceCompiler.ts` 薄桩 | ✅ | `compileMapSpaces` 补默认坐标并返回新 map |
| MapEditor 顶部展示校验错误（el-alert 列表） | ✅ | `topologyErrors` → `v-for` + `el-alert type="error"` |
| 校验失败禁用 Deploy | ✅ | `:disabled="!canDeploy"`；`deploy()` 内 `if (!canDeploy.value) return` |
| 地图变更自动重校验 | ✅ | `computed(() => validateTopology(store.project.map))` 绑定 reactive store |
| Deploy 链集成 spaceCompiler | ✅ | `compileMapForDeploy(compileMapSpaces(...))` |
| Tests：duplicate code、dangling edge | ✅ | test 文件 L28–49；另含 packId / 无节点 / 悬空连线 |
| 无 git commit | ✅ | 按 brief 要求 |
| Build green | ✅ | 复验见下 |

---

## Implementation quality

### Strengths

1. **校验分层清晰** — `validatePortRef` 抽取端口校验，错误码带前缀（`CONN_{id}_FROM_DEVICE` 等），便于定位。
2. **超出 brief 的合理扩展** — 空节点编码、无效端口格式、未知设备类型均有独立错误码，Deploy 前反馈更完整。
3. **响应式集成正确** — `computed` 绑定 `store.project.map`，无需额外 watch；packId / 节点 / 边 / 连线变更均自动重算。
4. **Deploy 双保险** — 按钮 disabled + 函数入口 guard，避免绕过 UI 触发 POST。
5. **spaceCompiler 占位恰当** — 不 mutate 原 map，与 II-2「设计态 / Deploy 态分离」一致；为后续 RCS 编译器预留插槽。
6. **测试可独立运行** — `npm run test:topology` 用 tsx 直跑，无 vitest 依赖，与 II-2 hitTest 风格一致。

### MapEditor 集成

- 错误 alert 位于标题与 toolbar 之间，Deploy 前可见性良好。
- `canDeploy` 仅依赖拓扑错误数量，逻辑单一、易读。

---

## Findings

### Critical

*None.*

### Minor（非阻塞）

1. **`el-alert :key="err.code"`** — 多条同码错误（如多个悬空边均为 `EDGE_TO_MISSING`）可能 Vue key 冲突；建议 `:key="\`${err.code}-${index}\`"` 或加入 edge/conn id。
2. **悬空边仅测 `to`** — 未断言 `EDGE_FROM_MISSING`；实现已有，补 1 条断言即可。
3. **无效端口格式 / 端口不存在** — 实现有 `*_FORMAT` / `*_PORT`，测试未覆盖；Phase III 可补。
4. **`spaceCompiler` 无单测** — 薄桩逻辑简单（`?? 0` 透传），brief 标 optional，可接受。
5. **重复编码多条目** — 三节点同 code 时只报一条 `DUPLICATE_NODE_CODE`；对 Deploy 阻断足够，UX 可后续增强。
6. **MapEditor 双空行风格** — 与 II-2 一致，不影响功能。

---

## Verification（review 复验）

```
npm run test:topology → ✅ topologyValidator: 6 cases passed
npm run test:hitTest  → ✅ hitTest: 8 assertions passed
npm run build         → ✅ exit 0 (vue-tsc + vite build)
```

---

## Recommendation

**Approve Task II-3.** Brief 所列交付物与 MapEditor 集成均已实现；拓扑校验规则完整，spaceCompiler 占位合理，测试与构建通过。Spec ✅，Quality **Approved**。Minor 项（alert key、补充 from-dangling / 端口格式测试）可在 III 联调前顺手补强。
