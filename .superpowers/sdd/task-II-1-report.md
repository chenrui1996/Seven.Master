# Task II-1 Report: Canvas 渲染内核

**Status:** ✅ Complete  
**Date:** 2026-08-30

## Deliverables

| File | Purpose |
|------|---------|
| `Seven.Simulator/src/components/map/MapCanvas.vue` | Canvas 绘制 nodes/edges，平移缩放，点击选中 |
| `Seven.Simulator/src/components/map/useMapCamera.ts` | 相机状态（offset/scale）与 screen↔world 变换 |
| `Seven.Simulator/src/components/map/hitTest.ts` | 纯函数节点命中检测（top-left 锚点，72×32） |
| `Seven.Simulator/src/components/map/hitTest.test.ts` | 8 条断言，经 `npm run test:hitTest` 运行 |
| `Seven.Simulator/src/views/MapEditor.vue` | 集成 MapCanvas（工具栏下方、表单上方） |

## Behavior

- **绘制：** 网格背景；边为节点中心连线；节点为圆角矩形 + code 标签；选中高亮（蓝底 + 亮边）
- **交互：** 拖拽背景平移；滚轮以指针为中心缩放（0.25×–4×）；单击选中节点并 `emit('select', id | null)`
- **Props：** `nodes`, `edges`, `selectedId`
- **MapEditor：** 选中节点时回填工具栏 code/x/y 字段；清空地图时清除选中

## Verification

```
npm run build          → ✅ green (vue-tsc + vite build)
npm run test:hitTest   → ✅ hitTest: 8 assertions passed
```

## Notes

- 节点坐标沿用 Phase I 语义：`x/y` 为矩形左上角，边连中心点
- 未引入 three.js / vitest；测试用 `npx tsx` 一次性拉取
- 无 git commit（按 brief 要求）
