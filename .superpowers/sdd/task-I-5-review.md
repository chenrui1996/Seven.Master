# Task I-5 Review: Simulator UI — Map / Player / Promote

**Reviewer:** Code review subagent  
**Date:** 2026-08-30  
**Scope:** Spec compliance + code quality (read-only; brief, report, diff, source verified)

---

## Verdicts

| Dimension | Result |
|-----------|--------|
| **Spec compliance** | ✅ |
| **Task quality** | **Approved** |

---

## Spec Compliance Checklist

| Requirement | Status | Notes |
|-------------|--------|-------|
| MapEditor: edge editor (from/to node selects, list, delete) | ✅ | `edgeFrom` / `edgeTo` selects bound to `map.nodes`; table + `removeEdge` |
| MapEditor: requestPoints form (code + mappedLocationCode) | ✅ | `addRequestPoint` / table / `removeRequestPoint` |
| MapEditor: keep Deploy / Undeploy | ✅ | Unchanged behaviour; payload `{ version, meta, map }` |
| MapEditor: `clearMap` clears `requestPoints` | ✅ | Line 84: `requestPoints = []` alongside nodes/edges/devices |
| Player: keep Triggers | ✅ | `simulateDestination` + `simulateFeedback` retained |
| Player: Reset → `POST /api/simulation/reset` | ✅ | `resetSimulation()` with `projectName` |
| Player: quick inbound add + approve | ✅ | `WmsInboundOrder/add` then `approve/{id}` |
| Player: auth blocked → clear ElMessage + README token docs | ✅ | `authHint()` on missing token / 401; README「认证」section |
| Promote: bind `project.promote.devices` | ✅ | Editable table on `store.project.promote.devices` |
| Promote: `promote-preview` then `promote` | ✅ | `onPreview` / `onPromote` with confirm dialog |
| Promote: reject / block loopback (`127.0.0.1` etc.) | ✅ | `isLoopbackHost` + `onHostBlur` clears; `validateDevices` blocks API |
| `importProject`: merge with `defaultSimProject()` | ✅ | `mergeSimProject()` in `importProject` |
| Pinia hydrate: legacy projects get scada / promote / simCommsMode | ✅ | `persist.afterHydrate` → `mergeSimProject(s.project)` |
| JWT interceptor reusing Vue3 `localStorage` key | ✅ | `AUTH_TOKEN_KEY = 'token'` matches `Seven.Vue3` user store |
| README: manual checklist + Reset / Promote API rows | ✅ | Phase I checklist; `/reset`, `/promote-preview`, `/promote` table rows |
| `npm run build` passes | ✅ | Accepted per report (`vue-tsc -b && vite build`); not re-run |
| No git commit | ✅ | Report + working tree only |
| Features.vue (if needed) | ✅ | No change required; store refactor re-exports schema types |

### Behaviour verified

| Concern | Implementation |
|---------|----------------|
| Schema centralization | `schema.ts` provides `defaultSimProject`, `mergeSimProject`, `isLoopbackHost`; store delegates |
| Trigger auth UX | Pre-flight `localStorage` check + 401 handler with same hint message |
| Reset scope | Anonymous simulation API; logs success/failure to Player log |
| Promote runtime mode | Sets `meta.runtimeMode = 'Production'` on success; local「切回仿真」 |
| Build gate fix | `tsconfig.app.json` adds `ignoreDeprecations: "6.0"` for TS 6 `baseUrl` warning |

---

## Findings

### Critical

*None.*

### Important

*None.*

### Minor

1. **Loopback can persist until blur** — `onHostBlur` clears loopback hosts; if user types `127.0.0.1` and navigates away without blur, value may persist in Pinia/export until next promote attempt. `validateDevices` still blocks API.

2. **Empty host allowed in UI** — `isLoopbackHost` only guards loopback literals; blank host passes UI validation (backend should reject on promote). Brief scoped loopback only.

3. **No duplicate edge / requestPoint guard** — Same from→to or duplicate request-point codes can be added. Not in brief.

4. **`schema.ts` outside I-5 diff stat** — `mergeSimProject` / `isLoopbackHost` live in `src/lib/project/schema.ts` (likely prior task); I-5 correctly wires store + views to them. Diff artifact lists 7 files (+469/−92).

5. **Diff artifact encoding** — `task-I-5-diff.txt` UTF-16 spaced characters; use report「Changes」+ source for authoritative scope.

6. **Reset has no confirm dialog** — Destructive-ish action fires immediately; acceptable for dev simulator, optional UX polish.

---

## Code Quality Notes

**Strengths**

- Focused diffs per view; MapEditor edge/requestPoint UI follows existing toolbar + card patterns.
- Auth story is coherent: shared `token` key, interceptor, proactive checks, README setup steps.
- `mergeSimProject` deep-merges nested `features.wcsPacks` and map arrays — handles legacy `.sevenproj.json` and Pinia hydrate in one function.
- Promote validation mirrors backend loopback rules (`127.0.0.1` / `localhost` / `::1`, trimmed, case-insensitive).
- Player quick inbound generates orderNo when blank; logs each step for联调 visibility.
- Type consolidation: store drops duplicated interfaces, re-exports from schema.

**Constraints verified**

- I-5 files: `MapEditor.vue`, `Player.vue`, `Promote.vue`, `project.ts`, `http.ts`, `README.md`, `tsconfig.app.json`.
- Gateway / 3D Player / in-app login correctly out of scope per report.

---

## Recommendation

**Approve Task I-5.** All brief checkboxes satisfied; UI wired to I-3/I-4 simulation APIs with sensible auth and legacy-project merge behaviour.

**Suggested follow-ups (optional, later phases):**

- Block loopback on input (`@change`) or before Pinia persist, not only on blur.
- Validate non-empty host before promote-preview.
- Duplicate edge / request-point dedup if editors become heavily used.
