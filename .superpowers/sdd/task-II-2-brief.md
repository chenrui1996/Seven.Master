# Task II-2: 设备库与连线

**Create:** `Seven.Simulator/src/components/map/deviceCatalog.ts` — types: SRM, CoordPoint, Conveyor, RequestPoint (with default ports if useful)

**MapEditor + MapCanvas:**
- Palette: drag (or click-to-place) device onto canvas → push `map.devices`
- Draw devices on canvas differently from location nodes
- Connections: allow linking `from: "deviceId.port"` → `to: "deviceId.port"` stored in `map.connections` (add to schema.ts if missing)
- Also fix II-1 Minor: when editing code/x/y form for selected node, write back to `map.nodes`

**Deploy adapter rule (document in schema comment):**
- For Phase II: on Deploy client-side (or note for backend later), `connections` involving RequestPoint may synthesize `requestPoints`; device-device connections may synthesize `edges` between nearest location nodes if both ends map to locations — keep rule simple and documented. Prefer client-side prep before POST deploy in MapEditor if backend doesn't read connections yet.

**No git commit. npm run build green.**

Work root: d:\Junheinrich\Junheinrich.Master\Seven.Master
