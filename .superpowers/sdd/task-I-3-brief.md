# Task I-3: Reset API

**Files:**
- Modify: `ISimulationDeployService` + `SimulationDeployService` + `SimulationController`
- Test: `SimulationDeployServiceTests` or new Reset tests

- [ ] Define `ResetAsync(projectName, ct)`: cancel/close unfinished Bus_TransportOrder / pack tasks for the `SIM_{project}` warehouse; do NOT delete Locations
- [ ] `POST /api/simulation/reset` body `{ projectName }`
- [ ] Test: after Deploy, Reset does not throw; Deployment status unchanged; Locations remain
- [ ] No git commit

Look up Bus transport order status enums and how to cancel. Prefer setting status to a cancelled/failed terminal state for open orders linked to that warehouse/locations if FK exists; if hard to link, cancel orders whose From/To location codes start with pack prefixes under that SIM warehouse's locations.

Work root: d:\Junheinrich\Junheinrich.Master\Seven.Master
