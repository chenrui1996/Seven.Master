### Task 11: 文档与演示开关

**Files:**
- Create `doc/19-WMS与WCS包.md` — short ops guide linking design docs; table prefixes; feature matrix; simulation TriggerPort note; Phase H deferred
- Update `doc/README.md`, `doc/14-功能开关.md` with Wms/OrchestrationBus/WcsPacks
- Ensure `HotStore:EnableDemoScheduler` remains false in appsettings
- Update `design/2026-08-29-wms-wcs-implementation-guide.md` §1.2 status note: Tasks 1–10 scaffolded (if true at time of writing)

**不要:** rewrite architecture; commit unless asked.

**Verify:** `dotnet test` filter Wms|Wcs|Platform|FourWay|Stacker|Orchestration|Stock|Order — should be green.
