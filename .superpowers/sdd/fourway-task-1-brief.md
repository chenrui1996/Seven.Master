### Task 1: Domain + EF (Policy / PutAway / RequestPoint)

**Files:** entities + enums + configs + DbContext + migration

**Produces:** `FwAislePolicy`, `FwAssignmentRecord`, `FwPutAwayTask`, `FwRequestPoint`, `FwPutAwayStatus`, `FwRequestPointType`, `FwLayerPolicy.AllocationWeight`

- [ ] **Step 1:** Add enums to `FourWayEnums.cs`:

```csharp
public enum FwPutAwayStatus
{
    Accepted = 0,
    LayerAssigned = 1,
    AisleAssigned = 2,
    LocationAssigned = 3,
    Completed = 4,
    Cancelled = 5,
    Failed = 6
}

public enum FwRequestPointType
{
    LayerRequest = 0,
    AisleRequest = 1,
    LocationRequest = 2
    // F5: Hoist* later
}
```

- [ ] **Step 2:** Create entities (fields per spec 搂4): `FwAislePolicy` (LayerCode, AisleCode, MinEmptySlots, MaxShuttleCount, DestinationPointCode, AllocationWeight, IsAvailable, MaxHeight, MaxWeight); `FwAssignmentRecord` (ScopeType Layer|Aisle, ScopeCode, LastAssignedAt, AssignCount); `FwPutAwayTask` (Id Guid, LegId, Container, From/To, Status, AssignedLayer/Aisle/Location); `FwRequestPoint` (Code, PointType, IsEnabled, LayerCode?, AisleCode?). Add `AllocationWeight` default 1 on `FwLayerPolicy`.

- [ ] **Step 3:** EF configs + DbSets + `dotnet ef migrations add AddFwPolicyPutAwayRequest`

- [ ] **Step 4:** Build solution; fix compile errors

---


