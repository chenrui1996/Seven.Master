### Task 7: 平台基础设施 — 接口日志、联锁

**表：**
- `Ifc_ApiLog`: Id, Direction (In/Out), SystemCode, CorrelationId?, LegId?, OrderNo?, Path?, RequestBody?, ResponseBody?, DurationMs, Success, ErrorMessage?, CreateDate
- `Ctl_Mode`: Id, Scope (Global/PackId), Mode (Auto/Semi/Manual), EStop (bool), UpdatedAt
- Optional single-row seed for Global Auto, EStop=false

**服务：**
- `IInterfaceLogService.WriteAsync(...)`
- `IControlModeService.GetAsync` / `SetModeAsync` / `SetEStopAsync`
- `IWcsPack.HealthAsync` / `AcceptLegAsync` should reject when EStop or Mode=Manual for that pack (update StackerWcsPack to check IControlModeService)

**API:** thin controllers under Platform or Wcs, feature-gated (Wms or OrchestrationBus or always when any WCS — use OrchestrationBus or new not required; gate with Wms||any pack — simplest: always register services, Controllers require auth)

**Tests:**
1. Write log then query/count
2. EStop true → Stacker AcceptLeg returns rejected/throws

**Migration:** `AddPlatformIfcCtl`
**不要：** full SCADA (Task 10); commit.
