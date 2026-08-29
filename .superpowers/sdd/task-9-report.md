# Task 9 Report: 外部 WCS 适配框架

**Branch:** `feature/wms-wcs-pack`  
**Date:** 2026-08-29  
**Status:** DONE  
**Commits:** none (per user rule)

---

## Summary

Implemented the External WCS adapter framework: transport + codec plug-in points, `ExternalWcsPack` implementing `IWcsPack`, message logging, and config-driven multi-instance registration.

- **Entities:** `ExtSystem` (`Ext_System`), `ExtMessageLog` (`Ext_MessageLog`)
- **Transport:** `IExternalTransport` → `HttpExternalTransport` (HttpClient POST), `MqExternalTransport` (throws when `Features.MessageQueue=false`, no-op when enabled)
- **Codec:** `IVendorCodec` + `FakeVendorCodec` (Leg ↔ JSON for tests)
- **Pack:** `ExternalWcsPack` — `AcceptLeg` encodes/sends/logs outbound; `HandleCallbackAsync` decodes inbound → `IOrchestrationBus.OnLegEventAsync`
- **DI:** `ExternalWcs` config array; one `IWcsPack` per enabled entry when `OrchestrationBus` or list non-empty
- **Migration:** `AddExternalWcsPack`

**Did not:** real STU/AGV vendor codecs, HTTP callback controller, git commit.

---

## TDD Evidence

### GREEN

**Command:**
```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~ExternalWcsPackTests"
```
**Outcome:** 3/3 passed

**Regression:**
```powershell
dotnet test ... --filter "FullyQualifiedName~ExternalWcsPackTests|FullyQualifiedName~FourWayPackTests|FullyQualifiedName~StackerPackTests|FullyQualifiedName~OrchestrationBusTests"
```
**Outcome:** 12/12 passed

| Test | Asserts |
|------|---------|
| `AcceptLeg_ShouldPostEncodedBody_AndLogOutbound` | FakeHttp captures JSON body; `Ext_MessageLog` Out/Success |
| `HandleCallback_Completed_ShouldCompleteOrderOnBus` | Callback → bus Completed → order Completed |
| `MqTransport_WhenMessageQueueDisabled_ShouldThrowNotSupported` | MQ off → NotSupportedException |

---

## Files Changed

| Action | Path |
|--------|------|
| Create | `Seven.Net8/Seven.Domain/Enums/ExternalWcsEnums.cs` |
| Create | `Seven.Net8/Seven.Domain/Entities/Wcs/External/ExtSystem.cs` |
| Create | `Seven.Net8/Seven.Domain/Entities/Wcs/External/ExtMessageLog.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Configuration/ExternalWcsOptions.cs` |
| Create | `Seven.Net8/Seven.Infrastructure/Wcs/External/*.cs` (transport, codec, pack, factory) |
| Create | `Seven.Net8/Seven.Infrastructure/Persistence/Configurations/Wcs/External/ExternalConfigurations.cs` |
| Modify | `Seven.Net8/Seven.Infrastructure/Persistence/SevenDbContext.cs` |
| Modify | `Seven.Net8/Seven.Infrastructure/Wcs/WcsServiceCollectionExtensions.cs` |
| Modify | `Seven.Net8/Seven.WebApi/appsettings.json` |
| Create | `Seven.Net8/Seven.Infrastructure/Migrations/20260829045925_AddExternalWcsPack.cs` |
| Create | `Seven.Net8/Seven.Tests/Wcs/ExternalWcsPackTests.cs` |

---

## Self-Review

| Check | Result |
|-------|--------|
| AcceptLeg posts encoded body via Http | OK |
| HandleCallback Completed → bus event | OK |
| Ext_ table prefixes | OK |
| Config `ExternalWcs` array + DI | OK |
| MQ stub respects MessageQueue feature | OK |
| Git commit | none |

---

## Concerns / Follow-ups

1. **No HTTP/MQ callback API endpoint** — `HandleCallbackAsync` is pack method only; WebApi controller needed for production ingress.
2. **`ExtSystem` entity not synced from config** — runtime driven by appsettings; DB table for admin UI later.
3. **Only `Fake` codec registered** — new vendors need `IVendorCodec` impl + DI registration.
4. **Mq transport is no-op** — real MassTransit publish not wired.
5. **`CanHandle` returns true when enabled** — multi external pack routing needs explicit PackId in leg planning.
