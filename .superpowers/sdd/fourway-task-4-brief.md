### Task 4: Inbound E2E + docs (F3楠屾敹)

**Files:** `InboundToFourWayE2ETests.cs`, `doc/24`, indexes

- [ ] **Step 1:** E2E: seed WH + Layer + Aisle + Locations `Fw.*` + LayerPolicy + AislePolicy + RequestPoint; BuildPallet via FourWay allocator; Bus+Pack+Destination; SimulateDestinationRequest; SegmentFeedback; assert stock at `Fw.*` target.

- [ ] **Step 2:** Assert pure Stacker leg not accepted by FourWay when both packs registered (CanHandle).

- [ ] **Step 3:** Write `doc/24-WCS鍥涘悜鍒嗛厤涓庡叆搴?md`; update `doc/19`, `doc/20`, `doc/README`, `design/shuttle-wcs/02` F2/F3 鉁? mark spec Sprint 1 done when green.

- [ ] **Step 4:** Run  
  `dotnet test --filter "FullyQualifiedName~FourWay|FullyQualifiedName~InboundToFourWay|FullyQualifiedName~InboundToStacker|FullyQualifiedName~StackerPack"`  
  Expect: PASS (Stacker regression green)

---


