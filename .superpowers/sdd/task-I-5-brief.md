# Task I-5: Simulator UI — Map / Player / Promote

**Files:** MapEditor.vue, Player.vue, Promote.vue, Features.vue (if needed), http/auth if JWT needed, README.md

- [ ] MapEditor: edit edges (from/to select nodes), requestPoints form; keep Deploy/Undeploy; clearMap should clear requestPoints too
- [ ] Player: keep Triggers; add Reset button → POST /api/simulation/reset; add quick inbound (add+approve) if feasible with current http client — if auth blocks, show clear ElMessage and document token setup
- [ ] Promote: bind project.promote.devices; call promote-preview then promote; reject/block UI save of 127.0.0.1
- [ ] importProject: merge missing fields with defaultSimProject() so old persisted projects get scada/promote/simCommsMode
- [ ] Update Seven.Simulator/README.md manual checklist + Reset/Promote API rows
- [ ] `npm run build` in Seven.Simulator must pass
- [ ] No git commit

Inspect existing http.ts and how Vue3 stores token — reuse localStorage key if same.

Work root: d:\Junheinrich\Junheinrich.Master\Seven.Master
