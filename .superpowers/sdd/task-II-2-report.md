# Task II-2 Report: 设备库与连线

**Status:** ✅ Complete  
**Date:** 2026-08-30

## Deliverables

| File | Purpose |
|------|---------|
| `Seven.Simulator/src/components/map/deviceCatalog.ts` | SRM / CoordPoint / Conveyor / RequestPoint 类型与默认端口 |
| `Seven.Simulator/src/lib/project/deployCompile.ts` | Deploy 前 `connections` → `edges` / `requestPoints` 编译 |
| `Seven.Simulator/src/lib/project/schema.ts` | `SimMapConnection`、`map.connections`、Deploy 规则注释 |
| `Seven.Simulator/src/components/map/hitTest.ts` | 设备命中检测（`hitTestDevice`） |
| `Seven.Simulator/src/components/map/MapCanvas.vue` | 设备/连线绘制；放置模式；拖放放置 |
| `Seven.Simulator/src/views/MapEditor.vue` | 设备库面板、连线表单、节点表单写回、Deploy 编译 |

## Behavior

- **设备库：** 四种设备类型，带默认端口；点击进入放置模式或拖拽到画布 → 写入 `map.devices`
- **绘制：** 节点（蓝矩形）与设备（按类型形状/颜色 + 端口圆点）区分；设备连线为橙色虚线（端口间）
- **连线：** `map.connections` 存储 `deviceId.portId` 格式；表单添加/删除
- **II-1 修复：** 选中节点后编辑 code/x/y 实时写回 `map.nodes`（`watch`）
- **Deploy 编译：** POST 前调用 `compileMapForDeploy`——RequestPoint 连线合成申请点；其它设备连线合成最近库位节点间的边

## Verification

```
npm run build          → ✅ green (vue-tsc + vite build)
npm run test:hitTest   → ✅ (未改断言，仍 8 条)
```

## Notes

- 设计态保留 `connections`；后端 Phase I DTO 不读此字段，编译结果仅用于 Deploy payload
- 无 git commit（按 brief 要求）
