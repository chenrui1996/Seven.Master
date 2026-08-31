# WCS-S2 Stacker Route Split — Spec

状态：已实现  
日期：2026-08-29  

## Goal

Package-internal routing for Stacker: `Stk_Route` graph, capacity-aware Dijkstra, merge consecutive `ExeStackCode` into `Stk_DeviceTask` segments, occupy/release `Stk_RouteFlow`, JudgeMap via `Stk_DeviceCoder`. Fallback to single segment when no routes.

## Out of scope

SupperRoute multi-seed / time windows; DeviceComm; FourWay.

## Acceptance

- Unit: path prefers low weight, skips full capacity; merge CV+SRM; segment advance; no-route fallback  
- Existing inbound/outbound Stacker E2E still green  
- Product doc `doc/22`
