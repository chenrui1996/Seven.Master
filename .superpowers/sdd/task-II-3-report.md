# Task II-3 Report: 拓扑校验与 Space 编译桩

**Status:** ✅ Complete  
**Date:** 2026-08-30

## Deliverables

| File | Purpose |
|------|---------|
| `Seven.Simulator/src/lib/map/topologyValidator.ts` | 校验 packId、节点数、唯一编码、边端点、连线端口 |
| `Seven.Simulator/src/lib/map/spaceCompiler.ts` | 薄桩：`compileMapSpaces` 用设备 x/y 补默认并透传 |
| `Seven.Simulator/src/lib/map/topologyValidator.test.ts` | 6 用例：合法图、重复编码、悬空边、缺 packId、无节点、悬空连线 |
| `Seven.Simulator/src/views/MapEditor.vue` | 顶部 `el-alert` 错误列表；校验失败禁用 Deploy；地图变更自动重算 |
| `Seven.Simulator/package.json` | 新增 `test:topology` 脚本 |

## Behavior

- **校验规则：** `packId` 非空；至少 1 个节点；节点 `code` 唯一；`edges.from/to` 指向现有节点 id；`connections` 两端为有效 `deviceId.portId`
- **MapEditor：** `computed` 绑定 `validateTopology`；工具栏上方展示错误；`:disabled="!canDeploy"` 阻止 Deploy
- **Deploy 链：** `compileMapSpaces` → `compileMapForDeploy` → POST

## Verification

```
npm run test:topology  → ✅ 6 cases passed
npm run test:hitTest   → ✅ 8 assertions passed
npm run build          → ✅ green (vue-tsc + vite build)
```

## Notes

- 无 git commit（按 brief 要求）
- `spaceCompiler` 为占位实现，未移植完整 RCS 编译器
