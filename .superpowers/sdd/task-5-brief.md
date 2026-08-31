### Task 5: 编排总线 TransportOrder / Leg

**Files:**
- Entities: `BusTransportOrder` → `Bus_TransportOrder`; `BusTransportLeg` → `Bus_TransportLeg`
- Optional: `WmsHandoverLink` → `Wms_HandoverLink` (FromPackId, ToPackId, LocationCode) for multi-pack planning
- `Seven.Infrastructure/Wcs/Bus/OrchestrationBus.cs` : `IOrchestrationBus`
- `IWcsPackResolver` — resolve by PackId or CanHandle
- `OrchestrationBusHostedService` — optional for V1: activate next pending leg; can be thin (OnLegEvent drives next)
- Fake packs in tests
- Wire `NoOpTransportOrderRequest` → real call to `IOrchestrationBus.CreateTransportOrderAsync` when Features.OrchestrationBus
- Migration `AddBusTransport`
- Test: `OrchestrationBusTests`

**State machines:**
- Order: Created → Planning → Executing → Completed | Failed | Cancelling
- Leg: Pending → Accepted → Running → Completed | Failed | Cancelled

**Planning V1:**
1. If exactly one registered `IWcsPack` and CanHandle(from,to) → single Leg
2. If multiple packs: use HandoverLink chain; if missing → Planning failed
3. CreateTransportOrder persists Order+Legs; Accept first Pending leg via pack.AcceptLegAsync

**OnLegEvent:**
- Completed → if more Pending legs, Accept next; else Order Completed + invoke `IWmsTransportCompletionHandler` (can be NoOp that just logs, or mark related inbound executing→completed hook)
- Failed → pause remaining legs; Order Failed

**Tests:**
1. Single FakePack: Create → Accept → OnLegEvent Completed → Order Completed
2. Two FakePacks + handover: Leg1 complete activates Leg2
3. Leg Failed stops subsequent

**DI:** Register bus when OrchestrationBus feature true; register empty pack list until Task 6; tests can new OrchestrationBus manually with fakes.

**不要：** Stacker real pack (Task 6); commit.
