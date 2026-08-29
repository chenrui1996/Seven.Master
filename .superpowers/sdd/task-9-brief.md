### Task 9: 外部 WCS 适配框架

**Entities (`Ext_`):**
- `ExtSystem`: PackId, Transport (Http|Mq), Codec, BaseUrl?, Enabled
- `ExtMessageLog`: SystemPackId, Direction, Payload, CorrelationId, Success

**Code under `Infrastructure/Wcs/External/`:**
- `IExternalTransport` with `HttpExternalTransport` (HttpClient) and stub `MqExternalTransport` (throws NotSupported or no-op publish if MQ off)
- `IVendorCodec` + `FakeVendorCodec` for tests (maps Leg ↔ JSON)
- `ExternalWcsPack` : IWcsPack — PackId from config instance; AcceptLeg encodes via Codec, sends via Transport, logs ExtMessageLog; expose method `HandleCallbackAsync` to complete leg via bus OnLegEvent
- Config section `ExternalWcs` array — bind list; register one ExternalWcsPack per enabled entry when OrchestrationBus or always when list non-empty

**Tests:**
1. FakeHttp (DelegatingHandler) + FakeCodec: AcceptLeg posts body
2. HandleCallback Completed → bus event (with Fake bus or real)

**不要：** real STU/AGV vendor; commit.
