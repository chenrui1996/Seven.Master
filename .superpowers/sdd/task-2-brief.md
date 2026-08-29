### Task 2: 包契约与通讯无关触发端口

**Files:**
- Create: `Seven.Net8/Seven.Application/Wcs/IWcsPack.cs`
- Create: `Seven.Net8/Seven.Application/Wcs/IOrchestrationBus.cs`
- Create: `Seven.Net8/Seven.Application/Wcs/IEquipmentTriggerPort.cs`
- Create: `Seven.Net8/Seven.Application/Wcs/WcsModels.cs`
- Create: `Seven.Net8/Seven.Infrastructure/Wcs/Triggers/InMemoryEquipmentTriggerPort.cs`
- Modify: `Seven.Net8/Seven.Infrastructure/Wcs/WcsServiceCollectionExtensions.cs` — 注册 Singleton `IEquipmentTriggerPort` → `InMemoryEquipmentTriggerPort`
- Test: `Seven.Net8/Seven.Tests/Wcs/EquipmentTriggerPortTests.cs`

**Interfaces (exact):**

```csharp
public interface IWcsPack
{
    string PackId { get; }
    Task<bool> CanHandleAsync(string fromLocationCode, string toLocationCode, CancellationToken ct = default);
    Task<AcceptLegResult> AcceptLegAsync(TransportLegDto leg, CancellationToken ct = default);
    Task CancelLegAsync(Guid legId, CancellationToken ct = default);
    Task<LegStatusDto?> QueryLegAsync(Guid legId, CancellationToken ct = default);
    Task<PackHealthDto> HealthAsync(CancellationToken ct = default);
}

public interface IOrchestrationBus
{
    Task<Guid> CreateTransportOrderAsync(CreateTransportOrderRequest req, CancellationToken ct = default);
    Task OnLegEventAsync(LegEvent evt, CancellationToken ct = default);
}

/// <summary>通讯无关：映射 LES_v2 的 SUDR/SUDS/SUM* 语义，由未来通讯包调用。</summary>
public interface IEquipmentTriggerPort
{
    event Func<DestinationRequestTrigger, Task>? DestinationRequested; // SUDR
    Task DispatchDestinationAsync(DispatchDestinationCommand cmd, CancellationToken ct = default); // SUDS
    Task DispatchMoveAsync(DispatchMoveCommand cmd, CancellationToken ct = default); // SUMT/SUMM/SUPM
    event Func<DeviceSegmentFeedback, Task>? SegmentFeedback; // SUMR/SUPR/SULL…
}
```

**WcsModels.cs 至少包含：**
- `TransportLegDto`：`LegId`,`OrderId`,`PackId`,`Seq`,`FromCode`,`ToCode`,`ContainerCode`,`HandoverIn`,`HandoverOut`
- `LegEvent`（含 LegId、事件类型/状态）
- `AcceptLegResult`
- `DestinationRequestTrigger`：`ContainerCode`,`SourcePointCode`,`Height`,`Weight`,`CheckResult`（可用 record 位置参数构造以匹配测试）
- `DispatchDestinationCommand`、`DispatchMoveCommand`、`DeviceSegmentFeedback`
- `CreateTransportOrderRequest`、`LegStatusDto`、`PackHealthDto`

**InMemoryEquipmentTriggerPort：**
- `SimulateDestinationRequestAsync` 触发 `DestinationRequested`
- `SimulateSegmentFeedbackAsync` 触发 `SegmentFeedback`
- `DispatchDestinationAsync` / `DispatchMoveAsync` 记录到可检查的列表（如 `DispatchedDestinations`）
- 线程安全足够测试用即可

**Test (required):**

```csharp
[Fact]
public async Task DestinationRequested_ShouldInvoke_Subscriber()
{
    var port = new InMemoryEquipmentTriggerPort();
    DestinationRequestTrigger? got = null;
    port.DestinationRequested += t => { got = t; return Task.CompletedTask; };
    await port.SimulateDestinationRequestAsync(new DestinationRequestTrigger(
        "TP001", "RP_IN_01", 1, 1, "OK"));
    got!.ContainerCode.Should().Be("TP001");
}
```

另加：`DispatchDestinationAsync` 后列表含该命令的测试。

**不要：** 实现真实 Bus/Pack；不要 commit；不要做 Task 3。
