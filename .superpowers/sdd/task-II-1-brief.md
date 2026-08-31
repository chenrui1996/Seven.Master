# Task II-1: Canvas 渲染内核

**Create:**
- `Seven.Simulator/src/components/map/MapCanvas.vue`
- `Seven.Simulator/src/components/map/useMapCamera.ts`
- `Seven.Simulator/src/components/map/hitTest.ts`
- Optional vitest or node test for hitTest pure function

**Behavior (rewrite, do not copy RCS mega-files):**
- Canvas draws nodes (rects/circles with code label) and edges (lines between node centers)
- Pan (drag background) + zoom (wheel)
- Click selects a node; emit `select` with node id
- Props: `nodes`, `edges`, `selectedId`

**Integrate:** MapEditor.vue shows MapCanvas above existing forms (keep form editing for Phase I fields).

**Constraints:** No three.js. No git commit. Keep build green (`npm run build`).

Work root: d:\Junheinrich\Junheinrich.Master\Seven.Master
