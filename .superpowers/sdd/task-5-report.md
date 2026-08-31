# Task 5 Report: 编排总线 TransportOrder / Leg

**Branch:** `feature/wms-wcs-pack`  
**Date:** 2026-08-29  
**Status:** DONE  
**Commits:** none (per user rule)

---

## Summary

Implemented the thin orchestration bus: persist `TransportOrder` + `Leg`, plan V1 (single pack or two-pack via `Wms_HandoverLink`), accept the first pending leg, and advance on `OnLegEvent`.

- Entities: `BusTransportOrder` → `Bus_TransportOrder`; `BusTransportLeg` → `Bus_TransportLeg`; `WmsHandoverLink` → `Wms_HandoverLink`
- Status: Order Created/Planning → Executing → Completed | Failed | Cancelling; Leg Pending → Accepted → Running → Completed | Failed | Cancelled
- `OrchestrationBus` + `WcsPackResolver` + thin `OrchestrationBusHostedService` (recovers stuck Pending; OnLegEvent already advances)
- `IWmsTransportCompletionHandler` NoOp (logs); last-leg Completed invokes it
- Feature on: register bus + replace `ITransportOrderRequest` with `BusTransportOrderRequest`; inbound `ReceiveAndBuildPallet` calls the hook
- Feature off: keep `NoOpTransportOrderRequest`
- Empty `IWcsPack` list until Task 6; tests construct `OrchestrationBus` with FakePacks
- EF migration `AddBusTransport`

**Did not** implement `StackerWcsPack`.

---

## TDD Evidence

### RED — Step 1

**Action:** Created `Seven.Tests/Wcs/OrchestrationBusTests.cs` (3 tests + FakePacks) before production types.

**Command:**
```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~OrchestrationBusTests" --no-restore
```

**Outcome:** Exit code **1** — compile errors (expected):
- `Seven.Domain.Entities.Bus` / `Seven.Infrastructure.Wcs.Bus` not found
- `OrchestrationBus` / `IWmsTransportCompletionHandler` / `BusTransportOrder` not found

### GREEN — Step 2

**Action:** Enums, entities, Fluent configs, DbSets, bus, resolver, hosted service, DI, inbound hook, migration.

**Command:**
```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~OrchestrationBusTests"
```

**Outcome:** Exit code **0**
```
已通过! - 失败: 0，通过: 3，已跳过: 0，总计: 3
```

**Regression:**
```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~WmsOrderServiceTests|FullyQualifiedName~StockServiceTests|FullyQualifiedName~OrchestrationBusTests"
```
```
已通过! - 失败: 0，通过: 9，已跳过: 0，总计: 9
```

---

## Files Changed

| Action | Path |
|--------|------|
| Create | `Seven.Net8/Seven.Domain/Enums/BusEnums.cs` |
| Create | `Seven.Net8/Seven.Domain/Wcs/BusDomainException.cs` |
| Create | `Seven.Net8/Seven.Domain/Entities/Bus/BusTransportOrder.cs` |
| Create | `Seven.Net8/Seven.Domain/Entities/Bus/BusTransportLeg.cs` |
| Create | `Seven.Net8/Seven.Domain/Entities/Wms/WmsHandoverLink.cs` |
| Create | `Seven.Net8/Seven.Application/Wcs/IWcsPackResolver.cs` |
| Create | `Seven.Net8/Seven.Application/Wms/IWmsTransportCompletionHandler.cs` |
| Modify | `Seven.Net8/Seven.Application/Wms/ITransportOrderRequest.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Wcs/Bus/OrchestrationBus.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Wcs/Bus/WcsPackResolver.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Wcs/Bus/OrchestrationBusHostedService.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Wcs/Bus/NoOpWmsTransportCompletionHandler.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Wcs/Bus/BusTransportOrderRequest.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Persistence/Configurations/Bus/BusConfigurations.cs` |
| Modify | `Seven.Net8/Seven.Infrastructure/Persistence/Configurations/Wms/WmsConfigurations.cs` |
| Modify | `Seven.Net8/Seven.Infrastructure/Persistence/SevenDbContext.cs` |
| Modify | `Seven.Net8/Seven.Infrastructure/Wcs/WcsServiceCollectionExtensions.cs` |
| Modify | `Seven.Net8/Seven.Infrastructure/Wms/WmsServiceCollectionExtensions.cs` |
| Modify | `Seven.Net8/Seven.Infrastructure/Wms/InboundOrderService.cs` |
| Modify | `Seven.Net8/Seven.Infrastructure/Wms/NoOpTransportOrderRequest.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Migrations/20260829044148_AddBusTransport.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Migrations/20260829044148_AddBusTransport.Designer.cs` |
| Modify | `Seven.Net8/Seven.Infrastructure/Migrations/SevenDbContextModelSnapshot.cs` |
| Create | `Seven.Net8/Seven.Tests/Wcs/OrchestrationBusTests.cs` |

---

## Implementation Notes

### Planning V1

1. Exactly one registered `IWcsPack` and `CanHandle(from,to)` → one Pending leg, then Accept.
2. Multiple packs: scan `Wms_HandoverLink`; if FromPack handles `from→handover` and ToPack handles `handover→to` → two legs; else Planning failed (`BusDomainException`, order persisted Failed).
3. Zero packs → Planning failed.

### OnLegEvent

- Completed → Accept next Pending; if none, Order Completed + `IWmsTransportCompletionHandler`
- Failed → remaining legs stay Pending (paused); Order Failed; handler not invoked
- Progress → Accepted/Pending → Running

### WMS wiring

`ReceiveAndBuildPallet` calls `ITransportOrderRequest` after the stock transaction. When `Features.OrchestrationBus` is true, that is `BusTransportOrderRequest` → `CreateTransportOrderAsync`. Hook `ToLocation` currently equals receive `ToLocation` (inbound line has no storage dest yet).

### DI

`AddSevenWcs` registers bus/resolver/completion/hosted service only when `OrchestrationBus` is true. No `IWcsPack` implementations. `AddSevenWms` (runs after) swaps `ITransportOrderRequest` to the bus adapter when the feature is on.

### Migration

```
dotnet ef migrations add AddBusTransport --project Seven.Net8/Seven.Infrastructure --startup-project Seven.Net8/Seven.WebApi --output-dir Migrations --context SevenDbContext
```

Tables: `Bus_TransportOrder`, `Bus_TransportLeg`, `Wms_HandoverLink`. Guid PKs use `ValueGeneratedNever` (InMemory + assigned Guids).

---

## Self-Review

| Check | Result |
|-------|--------|
| TDD RED→GREEN documented | OK |
| Single FakePack complete path | OK |
| Two FakePacks + handover activates Leg2 | OK |
| Leg Failed stops subsequent | OK |
| Table prefixes `Bus_` / `Wms_HandoverLink` | OK |
| Migration in `Infrastructure/Migrations/` | OK |
| No StackerWcsPack | OK |
| Feature-gated DI; empty pack list | OK |
| WMS inbound hook when feature on | OK |
| Git commit | none |

---

## Concerns / Follow-ups (Task 6+)

1. **Inbound From=To** — hook uses receive location as both ends; planning will fail until inbound has a real storage dest or Stacker `CanHandle` accepts same-location.
2. **Bus on + no packs** — inbound receive succeeds, then `CreateTransportOrder` throws `BusDomainException`. Default Features keep bus off.
3. **Handover V1 is one hop** — only two legs; longer chains not planned.
4. **Hook RefType/RefId** not mapped onto `CreateTransportOrderRequest` / order columns.
5. **Completion handler is NoOp** — does not move inbound Executing→Completed or relocate stock.
6. **Hosted service** only recovers stuck Pending; no timeout/QueryLeg yet.

---

## Out of Scope (confirmed not done)

- StackerWcsPack / any real `IWcsPack` (Task 6)
- Git commit
