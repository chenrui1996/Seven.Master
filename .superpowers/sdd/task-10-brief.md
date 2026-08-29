### Task 10: 2D SCADA 骨架

**Entities (`Scd_`):**
- `ScdView`: Id, Code, Name, Width, Height
- `ScdNodeBind`: Id, ViewId, LocationCode, X, Y, Label?

**API:** CRUD/query binds; SignalR optional later — V1 poll GET status joining Wms_Location.IsOccupied

**Vue:** `Seven.Vue3/src/views/Scada/Floor2d.vue` — simple canvas/div absolute positions from binds; read-only; feature flag — add `scada: true` under Features OR reuse Wms. Prefer `Features.Wms` gate for menu placeholder `ScadaFolder`.

**Tests:** API or service test create view+bind, list returns coordinates.

**Migration:** `AddScada2d`
**不要：** Three.js; commit.
