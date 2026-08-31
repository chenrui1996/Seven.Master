# Task III-3: 前端 Gateway 客户端 + Emulator 钩子

**Create:**
- `Seven.Simulator/src/lib/comms/simWcsProxy.ts` — SignalR client to `/hubs/sim-wcs-proxy`, methods sendWcsMessage, on OnWcsMessageReceived
- Optional minimal emulator: on message received, bump a reactive `lastMessage` and optionally nudge ThreeScene mesh position if show3d

**Player.vue:**
- When `meta.simCommsMode === 'Gateway'` (or a toggle), show Gateway panel: Connect / Disconnect / Send test payload
- Display last received message
- If 3D on, call a simple `nudgeDevice` on ThreeScene via ref/expose

Ensure vite proxies `/hubs` to WebApi like Vue3 does — check vite.config.ts.

No git commit. npm run build green.

Work root: d:\Junheinrich\Junheinrich.Master\Seven.Master
