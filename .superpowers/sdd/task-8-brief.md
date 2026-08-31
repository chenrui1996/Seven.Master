### Task 8: 四向车包 — 地图 / 寻路 / 交通（与堆垛机隔离）

**PackId:** `"fourway"`

**Entities (`Fw_` only):**
- `FwMapVersion`, `FwNode` (LocationCode link to Wms_Location.Code), `FwRoute` (from/to node, weight, capacity)
- `FwRouteGroup` optional minimal
- `FwShuttleTask`, `FwShuttleTaskPath` minimal

**Code under `Infrastructure/Wcs/Packs/FourWay/`:**
- `FourWayWcsPack` : IWcsPack
- `FourWayRouter` — weighted shortest path on small graph (pure functions preferred)
- `FourWayTrafficGuard` — minimal: reject head-on on same edge if occupied (HotStore key `fw:flow:{edgeId}` OR in-memory dict if HotStore feature off — prefer IHotStore when Features.HotStore else concurrent dictionary fallback for tests)
- HostedService optional
- Register when Features.WcsPacks.FourWay

**Hard isolation:** NO references to StackerAisleAllocator / Stk_ tables.

**Tests:**
1. Router finds path A→B on 3-node graph
2. Traffic: occupy edge, second grant on opposite direction fails
3. AcceptLeg creates FwShuttleTask (CanHandle true when pack enabled)

**Migration:** `AddFourWayPack`

**不要：** port entire FlowConflictAnalyzer; commit; physical comm.

Keep V1 traffic minimal but correct for head-on case.
