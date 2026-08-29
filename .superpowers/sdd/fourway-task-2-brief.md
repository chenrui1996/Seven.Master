### Task 2: Three-stage Allocator + Booking (F2)

**Files:** `FourWayInboundAllocator.cs` (+ optional helpers); `FourWayAllocatorTests.cs`

**Consumes:** Task 1 tables  
**Produces:** `AllocateInboundAsync` returns Layer/Aisle/Location and books location

- [ ] **Step 1:** Write failing tests in `FourWayAllocatorTests.cs`:
  - Prefer higher `FwLayerPolicy.AllocationWeight`
  - Skip aisle when empty slots &lt; MinEmptySlots
  - Book sets `WmsLocation.IsBooked`
  - Rotate using `FwAssignmentRecord`

- [ ] **Step 2:** Implement SelectLayer 鈫?SelectAisle 鈫?SelectLocation + Book in `FourWayInboundAllocator` (replace F1 skeleton). Height/weight filter on policies; layer/aisle availability from `Wms_Layer`/`Wms_Aisle` when present.

- [ ] **Step 3:** Run `dotnet test --filter FullyQualifiedName~FourWayAllocator` 鈥?expect PASS

---


