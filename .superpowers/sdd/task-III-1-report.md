# Task III-1 Report: 引入 three 与最小场景

**Status:** ✅ Complete  
**Date:** 2026-08-30

## Deliverables

| File | Purpose |
|------|---------|
| `Seven.Simulator/package.json` | 新增 `three`；dev 依赖 `@types/three` |
| `Seven.Simulator/src/components/player/ThreeScene.vue` | 最小 WebGL 场景：节点蓝箱、设备橙箱、GridHelper、OrbitControls |
| `Seven.Simulator/src/components/player/SimPlayerView.vue` | 3D 预览卡片，透传 `nodes` / `devices` |
| `Seven.Simulator/src/views/Player.vue` | `show3d` 开关（默认关）；开启时渲染 `SimPlayerView` |

## Behavior

- **坐标映射：** 2D 地图像素 × 0.01 → X/Z；节点/设备中心对齐 MapCanvas 锚点逻辑（`NODE_WIDTH/HEIGHT`、`deviceCenter`）
- **材质：** 节点 `#4a90d9`，设备 `#e67e22`；有内容时自动 `fitCamera`
- **生命周期：** `ResizeObserver` 自适应；卸载时 dispose geometry/material/renderer
- **数据：** props 来自 `store.project.map.nodes` / `devices`；deep watch 增量重建 mesh

## Verification

```
npm run build  → ✅ green (vue-tsc + vite build)
```

## Notes

- 无 git commit（按 brief 要求）
- 未实现 Gateway / RCS 动画，仅为 Phase III 最小占位场景
- Player chunk ~552 kB（含 three），后续可考虑 `import()` 懒加载
