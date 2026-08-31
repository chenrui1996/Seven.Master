# Final fix report — inbound demo path

Date: 2026-08-29  
Status: **done** (not committed)

## Tests

`dotnet test Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~Wms|FullyQualifiedName~Wcs"`

Passed: **26**, failed: **0** (includes `InboundToStackerE2ETests`, Stock, Order, Stacker, Bus).

## What changed

1. **ReceiveAndBuildPallet**: stock lands at receive loc (`ReceiveLocationCode` / line `FromLocation` / fallback `ToLocation`). Transport `from=receive → to=line.ToLocation`. Order stays **Executing** when bus transport is requested; still **Completed** after receive when OrchestrationBus/NoOp (same loc or feature off).
2. **WmsTransportCompletionHandler** (replaces NoOp when Wms+OrchestrationBus): Ship@From + Receive@To if needed; marks related InboundOrder Completed when Ref links exist (waits sibling TOs).
3. **IEquipmentTriggerPort.Simulate*** + `POST /api/Wcs/Triggers/destination-request` and `segment-feedback` (gated Stacker|FourWay|OrchestrationBus).
4. **InboundToStackerE2ETests**: Create→Approve→Receive@RECV targeting BIN→simulate SUDR/SUMR→stock at target + inbound Completed.
5. **Outbound** stub: optional `ITransportOrderRequest` on Approve when From≠To.

Migration: `20260829053000_AddInboundFromAndOutboundToLocation` (inbound FromLocation, outbound ToLocation).

## Remaining gaps

- Outbound still deducts on Ship immediately; no completion-handler ship/reserve wait.
- Trigger APIs have no HTTP-level test; e2e uses in-process port.
- Multi-line inbound OK if each line has its own TO; mixed Ref types not handled.
- LocationRequest (货位申请) path not covered by this e2e (AisleRequest only).
