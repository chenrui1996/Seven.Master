# Task I-5 Report: Simulator UI Phase I

**Status:** DONE  
**Date:** 2026-08-30  
**Commits:** none (per task constraint)

## Summary

Wired MapEditor (edges + requestPoints), Player (Reset + quick inbound), and Promote (preview/promote with loopback block). Fixed `importProject` / Pinia hydrate to merge defaults via `mergeSimProject()`. Added JWT interceptor reusing Vue3 `localStorage` key `token`. `npm run build` passes.

## Changes

### `src/lib/project/schema.ts`

- `mergeSimProject()` — deep-merge partial projects with `defaultSimProject()`
- `isLoopbackHost()` — UI guard aligned with backend

### `src/stores/project.ts`

- `importProject` uses `mergeSimProject`
- `persist.afterHydrate` merges legacy persisted state

### `src/api/http.ts`

- Request interceptor: `Authorization: Bearer` from `localStorage.token`

### `src/views/MapEditor.vue`

- Edge editor (from/to node selects, list, delete)
- Request points form (code + mappedLocationCode)
- `clearMap` clears `requestPoints`
- Deploy / Undeploy unchanged

### `src/views/Player.vue`

- **Reset** → `POST /api/simulation/reset`
- **快捷入库** → `WmsInboundOrder/add` + `approve/{id}` with auth hints on 401
- Triggers retained with token check

### `src/views/Promote.vue`

- Table bound to `project.promote.devices`
- **Promote 预览** → `promote-preview`; **Promote** → `promote`
- Blocks loopback hosts in UI (blur + validate before API)

### `README.md`

- Auth / token setup section
- Reset / promote-preview / promote API rows
- Phase I manual checklist

### `tsconfig.app.json`

- `ignoreDeprecations: "6.0"` (TS 6 baseUrl deprecation; build gate)

## Verification

```
cd Seven.Simulator
npm run build
  ✓ vue-tsc -b && vite build
```

## Out of Scope

- Git commit
- Gateway / 3D Player (later phases)
- Vue3 login page inside Simulator

## Concerns

None.
