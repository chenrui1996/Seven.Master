# Task III-2 Report: SignalR WCS Proxy Hub

**Status:** Done  
**Date:** 2026-08-30

## Summary

Implemented Phase III minimal Gateway backend: `SimWcsProxyHub` with RCS-aligned contract (`SendWcsMessage` / `OnWcsMessageReceived`), optional loopback TCP echo via `SimGatewayHostedService`, and config under `Simulator:Gateway`.

## Deliverables

| File | Purpose |
|------|---------|
| `Seven.Infrastructure/Simulator/Gateway/SimGatewayOptions.cs` | `Enabled`, `DefaultListenPort` (section `Simulator:Gateway`) |
| `Seven.Infrastructure/Simulator/Gateway/SimWcsProxyHub.cs` | Hub + `ISimWcsProxyNotifier` for TCP → SignalR push |
| `Seven.Infrastructure/Simulator/Gateway/SimGatewayMessage.cs` | Pure framing/echo helpers |
| `Seven.Infrastructure/Simulator/Gateway/SimGatewayHostedService.cs` | Loopback TCP echo; broadcasts via Hub |
| `SimulatorServiceCollectionExtensions.cs` | Options, notifier, conditional hosted service |
| `Program.cs` | `MapHub<SimWcsProxyHub>("/hubs/sim-wcs-proxy")` when `Features.SignalR` |
| `appsettings.Development.json` | Sample `Simulator:Gateway` config |
| `Seven.Tests/Simulator/SimGatewayTests.cs` | Message parser + hub construction tests |

## Wiring

- **Hub route:** `/hubs/sim-wcs-proxy` (gated by `Features.SignalR`, same as other hubs)
- **TCP echo:** `127.0.0.1:{DefaultListenPort}` only when `Simulator:Gateway:Enabled=true`
- **Auth:** `[AllowAnonymous]` on hub (aligns with `SimulationController`)

## Tests

```text
dotnet test --filter "FullyQualifiedName~SimulationDeploy|FullyQualifiedName~SimGateway"
→ 24 passed, 0 failed (17 SimulationDeploy + 7 SimGateway)
```

## Manual smoke (optional)

1. Run WebApi in Development (`Gateway:Enabled=true`, port 9100).
2. Connect TCP client to `127.0.0.1:9100`, send a line → receive echo.
3. Connect SignalR to `/hubs/sim-wcs-proxy`, invoke `SendWcsMessage("test-conn", "payload")` → receive `OnWcsMessageReceived`.

## Notes

- No git commit (per brief).
- Full Emulator / per-`wcsConnections[].serverPort` binding deferred to III-3.
