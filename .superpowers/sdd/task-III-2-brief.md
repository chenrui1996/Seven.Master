# Task III-2: SignalR WCS Proxy Hub（后端）

**Create under** `Seven.Net8/Seven.Infrastructure/Simulator/Gateway/`:
- `SimWcsProxyHub.cs` — Hub methods: `SendWcsMessage(string connectionId, string payload)` broadcasts `OnWcsMessageReceived` to callers (or group). Keep contract names aligned with RCS for frontend migration.
- `SimGatewayOptions` — `Enabled`, `DefaultListenPort` from config section `Simulator:Gateway`
- Optional: `SimGatewayHostedService` — if Enabled, start a TcpListener on loopback that echoes lines back as WCS messages via a static callback or IHubContext — **keep minimal**: echo server is enough for Phase III gate.

**Wire DI** in SimulatorServiceCollectionExtensions or Program.cs:
- Map hub at `/hubs/sim-wcs-proxy`
- Register options; hosted service only when Enabled=true

**Config** sample in appsettings.Development.json:
```json
"Simulator": { "Gateway": { "Enabled": true, "DefaultListenPort": 9100 } }
```

**Test:** at least a unit/integration test that hub can be constructed, OR a test for echo framing helper. If Tcp hard to test, test a pure `SimGatewayMessage` parser/echo helper.

No git commit. Don't break existing SimulationDeploy tests.

Work root: d:\Junheinrich\Junheinrich.Master\Seven.Master
