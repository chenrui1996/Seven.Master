### Task 5: PathDispatcher (F4)

- [x] `FourWayPathDispatcher`: load `Fw_Route` for MapVersion of layer; Dijkstra via `FourWayRouter`; write `Fw_ShuttleTaskPath`; `TrafficGuard.TryGrant` / Release on segment advance
- [x] Wire Destination SegmentFeedback to advance paths
- [x] Fallback single segment when no routes
- [x] Tests: path grant/release; no-map fallback
