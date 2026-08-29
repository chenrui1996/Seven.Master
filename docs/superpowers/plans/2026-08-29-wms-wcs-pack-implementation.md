# 立库 WMS + 可插拔 WCS 包 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 Seven.Master 单进程内落地「WMS 账本 + 编排总线 + 可插拔 WCS 包」，先打通堆垛机入库/出库主路径（通讯语义触发、物理通讯后置），再接入四向车包、外部适配与平台基础设施。

**Architecture:** 对齐 `design/2026-08-29-wms-wcs-pack-architecture.md`：WMS 管库位级账本；薄编排总线拆 `TransportOrder`/`Leg`；`Stacker` / `FourWay` / `External` 为独立包（路径/流量/分配算法隔离）；设备触发用通讯无关端口（SUDR/SUDS 等语义），真实 Socket/Modbus/S7 通讯包装下阶段。

**Tech Stack:** .NET 8（Seven.Domain / Application / Infrastructure / Business / WebApi）、EF Core、现有 `ICacheService`/`IHotStore`/`DeviceComm`（后置接入）、Vue3 + Pinia + Element Plus、xUnit + FluentAssertions、Features 门控。

## Global Constraints

- 规格：`design/2026-08-29-wms-wcs-pack-architecture.md`；缓存：`design/entity-cache-implementation-outline.md`
- **技术栈与分层以 Seven.Master 为准**；禁止引入 LES2/LES_v2/RCS4Shuttle 程序集引用，只迁领域逻辑
- **表前缀按模块强制隔离**（Fluent `ToTable`，禁止无前缀混用）：

| 模块 | 前缀 | 示例 |
|------|------|------|
| WMS | `Wms_` | `Wms_InboundOrder`、`Wms_Stock`、`Wms_Location` |
| 编排总线 | `Bus_` | `Bus_TransportOrder`、`Bus_TransportLeg` |
| 堆垛机 WCS | `Stk_` | `Stk_PutAwayTask`、`Stk_DeviceTask`、`Stk_AssignmentPolicy` |
| 四向车 WCS | `Fw_` | `Fw_Route`、`Fw_RouteFlow`、`Fw_ShuttleTask` |
| 外部 WCS | `Ext_` | `Ext_System`、`Ext_MessageLog` |
| 接口日志 | `Ifc_` | `Ifc_ApiLog` |
| 联锁/单控 | `Ctl_` | `Ctl_Interlock`、`Ctl_Mode` |
| 2D SCADA | `Scd_` | `Scd_View`、`Scd_NodeBind` |

- HotStore Key 必须带包前缀：`stk:` / `fw:` / `bus:`（禁止再用 Demo 全局 `wcs:` 作为生产键）
- WMS 单据：**仅一种入库单、一种出库单、一种盘点单**（`OrderType` 区分业务来源）；参考 `D:\Junheinrich\Project\FJD\SrcCode\LES2`，舍弃多套同构表
- 堆垛机流程参考 `D:\Logbot\1 Project\Projects_2019\LB-PN147A_ASD\04_Software\trunk\SourceCode\LES_v2`：保留「目的地申请→巷道/货位分配→Transport→设备段→目的地下发」；**巷道/货位分配只存在于 `Stk_`**，禁止与四向车共用
- 四向车参考 `D:\Junheinrich\Shuttle\RCS4Shuttle`：寻路/交通管制进 `Fw_` + 包内 HotStore；禁止与 `Stk_` 共享路网表
- **本阶段不实现多仓多活写 PLC**；不绑定 Socket/Modbus/S7 具体驱动。使用 `IEquipmentTriggerPort` 表达 SUDR/SUDS/SUMT/SUMR/SUPR/SULL 等语义；通讯包装为 **Phase H（下阶段）**
- V1 业务任务 : TransportOrder = **1:1**；波次 1:N 以后再说
- Stacker V1 设备子集：输送线 + 堆垛机；RGV 作为同包后续增量，不阻塞主路径
- 提交：仅在用户明确要求时 `git commit`
- Working directory：`Seven.Master`（仓库根）
- EF 迁移统一输出到 `Seven.Net8/Seven.Infrastructure/Migrations/`（与现有 ModelSnapshot 一致，勿再写入 `Persistence/Migrations`）

## Scope / 子计划拆分

本文件为 **总计划**。体量过大时按 Phase 拆独立详细计划（仍遵守本 Global Constraints）：

| Phase | 可独立交付 | 建议子计划文件（需要时再建） |
|-------|------------|------------------------------|
| A | 功能开关 + 契约 + 表前缀脚手架 | （本文件 Task 1–2） |
| B | WMS 瘦身账本 + 三单 | `…-wms-core.md` |
| C | 编排总线 | `…-orchestration-bus.md` |
| D | 堆垛机包（语义触发） | `…-stacker-pack.md` |
| E | 平台基础设施（接口日志/异常/联锁） | `…-wcs-platform-infra.md` |
| F | 四向车包 | `…-fourway-pack.md` |
| G | 外部 WCS 适配框架 | `…-external-wcs.md` |
| H | 通讯包（Socket/Modbus/S7，多活） | **下阶段**，本计划只留端口 |
| I | 2D SCADA | `…-scada-2d.md` |

---

## File Structure（目标落位）

| 路径 | 职责 |
|------|------|
| `Seven.Net8/Seven.Application/Wcs/IWcsPack.cs` | 包契约 |
| `Seven.Net8/Seven.Application/Wcs/IOrchestrationBus.cs` | 总线契约 |
| `Seven.Net8/Seven.Application/Wcs/IEquipmentTriggerPort.cs` | 通讯无关触发端口（SUDR/SUDS 语义） |
| `Seven.Net8/Seven.Application/Wms/*.cs` | WMS 服务接口 |
| `Seven.Net8/Seven.Domain/Entities/Wms/` | `Wms_*` 实体 |
| `Seven.Net8/Seven.Domain/Entities/Bus/` | `Bus_*` 实体 |
| `Seven.Net8/Seven.Domain/Entities/Wcs/Stacker/` | `Stk_*` 实体 |
| `Seven.Net8/Seven.Domain/Entities/Wcs/FourWay/` | `Fw_*` 实体 |
| `Seven.Net8/Seven.Domain/Entities/Platform/` | `Ifc_`/`Ctl_`/`Scd_` |
| `Seven.Net8/Seven.Infrastructure/Wms/` | WMS 服务实现 |
| `Seven.Net8/Seven.Infrastructure/Wcs/Bus/` | 总线 |
| `Seven.Net8/Seven.Infrastructure/Wcs/Packs/Stacker/` | 堆垛机包 |
| `Seven.Net8/Seven.Infrastructure/Wcs/Packs/FourWay/` | 四向车包 |
| `Seven.Net8/Seven.Infrastructure/Wcs/External/` | 外部 Transport+Codec |
| `Seven.Net8/Seven.Infrastructure/Wcs/Triggers/` | 内存/仿真 TriggerPort |
| `Seven.Net8/Seven.Infrastructure/Configuration/AppOptions.cs` | Features / WcsPacks / ExternalWcs |
| `Seven.Net8/Seven.WebApi/Controllers/Wms/`、`Wcs/` | API |
| `Seven.Vue3/src/views/Wms/`、`Wcs/Stacker/`、`Wcs/FourWay/`、`Scada/` | UI |
| `Seven.Net8/Seven.Tests/Wms/`、`Wcs/` | 单测/集成测 |
| `doc/19-WMS与WCS包.md` | 产品文档（实现后） |

---

### Task 1: Features、表前缀常量与 DI 骨架

**Files:**
- Modify: `Seven.Net8/Seven.Infrastructure/Configuration/AppOptions.cs`
- Create: `Seven.Net8/Seven.Domain/Wcs/TablePrefixes.cs`
- Create: `Seven.Net8/Seven.Infrastructure/Wcs/WcsServiceCollectionExtensions.cs`
- Modify: `Seven.Net8/Seven.Infrastructure/DependencyInjection.cs`
- Modify: `Seven.Net8/Seven.WebApi/appsettings.json`（及 Development）
- Modify: `Seven.Vue3/src/stores/features.ts`
- Test: `Seven.Net8/Seven.Tests/Wcs/FeatureOptionsWcsTests.cs`

**Interfaces:**
- Consumes: 现有 `FeatureOptions.IsEnabled`
- Produces: `FeatureOptions.Wms`、`OrchestrationBus`、`WcsPacks`（`Stacker`/`FourWay`/`BoxSort`）；`AddSevenWcs(IConfiguration)`

- [ ] **Step 1: 写失败测试**

```csharp
using FluentAssertions;
using Seven.Infrastructure.Configuration;
using Xunit;

namespace Seven.Tests.Wcs;

public class FeatureOptionsWcsTests
{
    [Fact]
    public void IsEnabled_ShouldRead_NestedPack_ViaFlatName_OrExplicit()
    {
        var f = new FeatureOptions
        {
            Wms = true,
            OrchestrationBus = true,
            WcsPacks = new WcsPackFeatureOptions { Stacker = true, FourWay = false }
        };
        f.Wms.Should().BeTrue();
        f.WcsPacks.Stacker.Should().BeTrue();
        f.WcsPacks.FourWay.Should().BeFalse();
    }
}
```

- [ ] **Step 2: 跑测试确认需扩展类型**

```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~FeatureOptionsWcsTests"
```

Expected: 编译失败（缺少属性）

- [ ] **Step 3: 增加表前缀常量**

```csharp
namespace Seven.Domain.Wcs;

public static class TablePrefixes
{
    public const string Wms = "Wms_";
    public const string Bus = "Bus_";
    public const string Stacker = "Stk_";
    public const string FourWay = "Fw_";
    public const string External = "Ext_";
    public const string InterfaceLog = "Ifc_";
    public const string Control = "Ctl_";
    public const string Scada = "Scd_";
}
```

- [ ] **Step 4: 扩展 FeatureOptions**

在 `AppOptions.cs` 的 `FeatureOptions` 中增加：

```csharp
public bool Wms { get; set; }
public bool OrchestrationBus { get; set; }
public WcsPackFeatureOptions WcsPacks { get; set; } = new();

public class WcsPackFeatureOptions
{
    public bool Stacker { get; set; }
    public bool FourWay { get; set; }
    public bool BoxSort { get; set; }
}
```

注意：现有 `IsEnabled("DeviceComm")` 靠反射读 **bool 属性**；包开关用 `WcsPacks.Stacker` 时，Controller 用显式检查或新增 `IsWcsPackEnabled("Stacker")`，不要把嵌套对象塞进 `IsEnabled`。

- [ ] **Step 5: `AddSevenWcs` 空壳**

```csharp
public static class WcsServiceCollectionExtensions
{
    public static IServiceCollection AddSevenWcs(this IServiceCollection services, IConfiguration configuration)
    {
        var features = configuration.GetSection(FeatureOptions.SectionName).Get<FeatureOptions>() ?? new();
        // Phase A：仅注册占位；后续 Task 挂 Bus/Packs
        return services;
    }
}
```

在 `AddSevenInfrastructure` 末尾调用 `services.AddSevenWcs(configuration)`。

- [ ] **Step 6: appsettings + 前端 flags**

`Features` 节增加 `Wms`/`OrchestrationBus`/`WcsPacks`；默认全 `false`。  
`features.ts` 增加对应字段与 `menuFeatureMap` 占位（菜单种子在后续 Task）。

- [ ] **Step 7: 跑测试通过**

```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~FeatureOptionsWcsTests"
```

Expected: PASS

- [ ] **Step 8: Commit**（仅当用户要求）

```bash
git add Seven.Net8/Seven.Domain/Wcs Seven.Net8/Seven.Infrastructure/Configuration/AppOptions.cs Seven.Net8/Seven.Infrastructure/Wcs Seven.Net8/Seven.Tests/Wcs Seven.Vue3/src/stores/features.ts
git commit -m "feat(wcs): add feature flags and table prefix constants"
```

---

### Task 2: 包契约与通讯无关触发端口

**Files:**
- Create: `Seven.Net8/Seven.Application/Wcs/IWcsPack.cs`
- Create: `Seven.Net8/Seven.Application/Wcs/IOrchestrationBus.cs`
- Create: `Seven.Net8/Seven.Application/Wcs/IEquipmentTriggerPort.cs`
- Create: `Seven.Net8/Seven.Application/Wcs/WcsModels.cs`
- Create: `Seven.Net8/Seven.Infrastructure/Wcs/Triggers/InMemoryEquipmentTriggerPort.cs`
- Test: `Seven.Net8/Seven.Tests/Wcs/EquipmentTriggerPortTests.cs`

**Interfaces:**
- Produces:

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

- [ ] **Step 1: 定义 DTO（`WcsModels.cs`）**  
  至少包含：`TransportLegDto`（`LegId`,`OrderId`,`PackId`,`Seq`,`FromCode`,`ToCode`,`ContainerCode`,`HandoverIn`,`HandoverOut`）、`LegEvent`、`AcceptLegResult`、`DestinationRequestTrigger`（`ContainerCode`,`SourcePointCode`,`Height`,`Weight`,`CheckResult`）、`DispatchDestinationCommand`、`DeviceSegmentFeedback`。

- [ ] **Step 2: 实现 `InMemoryEquipmentTriggerPort`**  
  用 `Channel`/`event` 在测试中注入 SUDR，断言业务订阅被调用；`DispatchDestinationAsync` 记录下发列表供断言。

- [ ] **Step 3: 测试**

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

- [ ] **Step 4: 注册 TriggerPort 为 Singleton（仿真默认）**；真实通讯包装 Phase H 替换。

---

### Task 3: WMS 主数据与账本（Stock / Location / Container）

**参考迁入（逻辑，非照搬字段）：**  
`LES2\LES.Entities\Models\BasicData\Stock.cs`、`Location.cs`、`Container.cs`、`Warehouse.cs`、`Zone.cs`  
舍弃：项目专属预留客户/销售组织等；保留库位坐标、占用、锁定、容器绑定。

**Files:**
- Create: `Seven.Net8/Seven.Domain/Entities/Wms/WmsWarehouse.cs` 等
- Create: `Seven.Net8/Seven.Infrastructure/Persistence/Configurations/Wms/*.cs`（全部 `ToTable(TablePrefixes.Wms + "…")`）
- Create: `Seven.Net8/Seven.Application/Wms/IStockService.cs`、`ILocationService.cs`、`IContainerService.cs`
- Create: `Seven.Net8/Seven.Infrastructure/Wms/*Service.cs`
- Modify: `Seven.Net8/Seven.Infrastructure/Persistence/SevenDbContext.cs`
- Migration: `AddWmsMasterAndStock`
- Test: `Seven.Net8/Seven.Tests/Wms/StockServiceTests.cs`

**实体最小集：**

| 实体类 | 表名 |
|--------|------|
| `WmsWarehouse` | `Wms_Warehouse` |
| `WmsZone` | `Wms_Zone` |
| `WmsLocation` | `Wms_Location` |
| `WmsContainer` | `Wms_Container` |
| `WmsContainerType` | `Wms_ContainerType` |
| `WmsStock` | `Wms_Stock` |
| `WmsStockLedger` | `Wms_StockLedger`（流水） |

- [ ] **Step 1: 失败测试** — `Allocate`/`Receive`/`Ship` 在空库上的不变量（数量守恒、库位占用）。
- [ ] **Step 2: 实体 + Fluent 配置 + DbSet + 迁移**
- [ ] **Step 3: `StockService` 命令只写 DB；投影缓存按实体缓存大纲通道 B 后续再加，本 Task 可不接 Cache**
- [ ] **Step 4: API** `WmsLocationsController` / `WmsStocksController` 基础 CRUD 查询（`[RequiresFeature]` 改用 `Features.Wms` 显式检查）
- [ ] **Step 5: 测试通过**

**硬规则：** `Wms_Location` 是全局库位权威（含交接位）；`Stk_`/`Fw_` **不得**再建第二套货位主数据表。堆垛机巷道策略表可引用 `Wms_Location.Code`。

---

### Task 4: WMS 三单收敛（入库 / 出库 / 盘点）

**参考：** LES2 多套 `*InboundOrder*` / `*OutboundOrder*` / `Cyclecount*`  
**目标：** 各 1 套 Header+Line（Detail 按需）；`OrderType` 枚举区分来源。

**Files:**
- Create: `WmsInboundOrder` / `WmsInboundOrderLine` → `Wms_InboundOrder*`
- Create: `WmsOutboundOrder` / `WmsOutboundOrderLine` → `Wms_OutboundOrder*`
- Create: `WmsCycleCount` / `WmsCycleCountLine` / `WmsCycleCountRecord` → `Wms_CycleCount*`
- Create: `Seven.Infrastructure/Wms/InboundOrderService.cs` 等
- Create: 领域状态机枚举 `WmsOrderStatus`
- Test: 入库收货→组盘→生成运输请求；出库分配→预留；盘点差异调账

**舍弃：** Purchase/Production/Transfer… 分表；Waybill 首期不做；空托单用 `OrderType=EmptyPallet`；库存属性变更四单首期不做（或仅提供库存调整 API）。

**修正流程（相对 LES2）：**

1. 入库：审核 → 组盘（Container+Stock 在收货区）→ 调用总线 `CreateTransportOrder(from=收货位,to=目标库位或包内再申请)` → 完成回写  
2. 出库：审核 → 库存预留 → TransportOrder → 发运扣账  
3. 盘点：计划 → 实盘录入 → 差异确认 → 调账  

- [ ] **Step 1–N:** 按 TDD 落地三单服务；每单种至少一个集成测试  
- [ ] **Step 末:** 菜单种子 `WmsFolder` + Vue 基础列表页（可先用生成页配置，扩展点留着）

---

### Task 5: 编排总线 TransportOrder / Leg

**Files:**
- Create: `BusTransportOrder` → `Bus_TransportOrder`；`BusTransportLeg` → `Bus_TransportLeg`
- Create: `Seven.Infrastructure/Wcs/Bus/OrchestrationBus.cs`
- Create: `Seven.Infrastructure/Wcs/Bus/OrchestrationBusHostedService.cs`（激活下一段、超时扫描）
- Create: `IWcsPackResolver`（按 PackId / CanHandle）
- Test: `OrchestrationBusTests` — 单段完成；两段交接；失败暂停后续

**状态机：**  
Order: `Created → Planning → Executing → Completed|Failed|Cancelling`  
Leg: `Pending → Accepted → Running → Completed|Failed|Cancelled`

**规划规则 V1：**

1. 若仅启用一个包且 `CanHandle(from,to)` → 单 Leg  
2. 若跨包 → 查 `Wms_Location` 上标记 `IsHandover=true` 的交接链（主数据配置表可加 `Wms_HandoverLink`：FromPack,ToPack,LocationCode）  
3. 无交接配置 → Planning 失败  

- [ ] **Step 1: 测试两包 FakePack 交接**  
- [ ] **Step 2: 实现 Bus + 迁移**  
- [ ] **Step 3: WMS 入库完成组盘后调用 `CreateTransportOrderAsync`**  
- [ ] **Step 4: `OnLegEvent(Completed)` 在最后一段触发 WMS 落账回调接口 `IWmsTransportCompletionHandler`**

---

### Task 6: 堆垛机包 — 分配与任务（无物理通讯）

**参考流程（LES_v2）：**  
`DestinationRequestService` + `LocationExecuteStack.SelectAisle/SelectLocation` + `TransportTask`/`DeviceExecTask` + SUDR/SUDS 语义  

**Files（均在 `Infrastructure/Wcs/Packs/Stacker/`）：**
- `StackerWcsPack.cs` : `IWcsPack`，`PackId = "stacker"`
- `StackerDestinationService.cs` — 订阅 `IEquipmentTriggerPort.DestinationRequested`
- `StackerAisleAllocator.cs` / `StackerLocationAllocator.cs` — **仅用 `Stk_` 策略表**
- `StackerRouteService.cs` — 包内简单节点路径（输送点序列），**不是**四向车路网
- `StackerSchedulerHostedService.cs`
- Entities: `Stk_RequestPoint`、`Stk_AssignmentPolicy`、`Stk_AssignmentRecord`、`Stk_PutAwayTask`、`Stk_DeviceTask`、`Stk_Route`（可选）

**禁止：** 引用 `Fw_Route` / FourWay HotStore；把 SelectAisle 逻辑放到 WMS。

**触发映射：**

| 语义 | 端口方法 | 原报文 |
|------|----------|--------|
| 目的地申请 | `DestinationRequested` | SUDR |
| 目的地下发 | `DispatchDestinationAsync` | SUDS |
| 堆垛机任务 | `DispatchMoveAsync` | SUMT… |
| 反馈 | `SegmentFeedback` | SUMR/SUPR/SULL |

**RequestPoint 类型（枚举迁入并净化）：** AisleRequest / LocationRequest / BlockingPoint / AP / EP  

- [ ] **Step 1: 单元测试 `SelectAisle` 轮转与高重过滤**（用例从 LES_v2 提炼，去掉硬编码巷道 Id）  
- [ ] **Step 2: 测试 SUDR→分配→DispatchDestination 被调用**（InMemory port）  
- [ ] **Step 3: AcceptLeg 创建 `Stk_PutAwayTask`/`Stk_DeviceTask`，完成后 `OnLegEvent(Completed)`**  
- [ ] **Step 4: Features.WcsPacks.Stacker=true 时注册 Pack + HostedService**  
- [ ] **Step 5: 端到端测试：WMS 入库单 → Bus → Stacker → 仿真 SUDR/SUDS → 库存到目标库位**

---

### Task 7: 平台基础设施 — 接口日志、异常、联锁

**Files:**
- `Ifc_ApiLog`：方向(In/Out)、系统、关联 `LegId`/`OrderNo`、请求/响应体、耗时、成功标记  
- `IInterfaceLogService` + 中间件或装饰外部 HttpClient Handler  
- 统一异常：`WmsException` / `WcsPackException` → ProblemDetails；告警码 `WMS*`/`BUS*`/`WCS.stacker*`  
- `Ctl_Interlock` / `Ctl_Mode`：系统自动/半自动/手动；急停；按包或全局禁止接单（`IWcsPack.Health` 读联锁）

- [ ] **Step 1: Ifc 日志写入测试**  
- [ ] **Step 2: 联锁为 Off 时 AcceptLeg 拒绝**  
- [ ] **Step 3: 管理 API + 简单 Vue 页**

（2D SCADA 放到 Task 10，避免本 Task 膨胀。）

---

### Task 8: 四向车包 — 地图 / 寻路 / 交通（与堆垛机隔离）

**参考概念：** RCS4Shuttle `SupperRoute`、`FlowConflictAnalyzer`（V2 几何分类优先）  
**落位：** `Infrastructure/Wcs/Packs/FourWay/`  

**表：** `Fw_MapVersion`、`Fw_Node`（可关联 `Wms_Location.Code`）、`Fw_Route`、`Fw_RouteGroup`、`Fw_ShuttleTask`、`Fw_ShuttleTaskPath`  
**热态：** HotStore keys `fw:flow:{edgeId}`、`fw:veh:{id}`；预热读 `Fw_*` 冷配置  

**硬隔离：**

- 分配货位：四向车用本包策略（深位/近端等），**禁止调用** `StackerAisleAllocator`
- 输送线/提升机属四向车包设备域，与 Stacker 输送线配置分离

- [ ] **Step 1: 纯函数寻路单测（小图最短/绕行比）**  
- [ ] **Step 2: 对向独享段 Grant 单测（从 FCA 文档提炼 2–3 个死锁回归场景）**  
- [ ] **Step 3: `FourWayWcsPack` + HostedService + AcceptLeg**  
- [ ] **Step 4: 与总线双包交接测试（Stacker→Handover→FourWay）**  
- [ ] **Step 5: 设备触发仍走 `IEquipmentTriggerPort`（或包内专用 Port）；物理通讯 Phase H**

---

### Task 9: 外部 WCS 适配框架

**Files:**
- `Ext_System`、`Ext_MessageLog`
- `IExternalTransport`（Http | Mq）
- `IVendorCodec`
- `ExternalWcsPack` : `IWcsPack`
- 配置节 `ExternalWcs: []`（PackId、Transport、Codec、连接）

- [ ] **Step 1: FakeHttpTransport + FakeCodec 往返测试（下发 Leg → 回调 Completed）**  
- [ ] **Step 2: MQ Transport 骨架（依赖 Features.MessageQueue）**  
- [ ] **Step 3: 文档说明如何加新供应商 Codec**

真实 STU/AGV Codec 按项目另开任务，本框架只保证插拔。

---

### Task 10: 2D SCADA 骨架

**Files:**
- `Scd_View`、`Scd_NodeBind`（绑定 `Wms_Location` 或设备 Code + 画布坐标）
- Vue：`views/Scada/Floor2d.vue` — 只读展示库位状态/任务高亮（SignalR 推送）
- 数据源：WMS 库位状态 + 已启用包 Health/忙闲；**不做**三维

- [ ] **Step 1: 绑定 API + 前端画布只读**  
- [ ] **Step 2: 订阅 Bus Leg 事件刷新**  
- [ ] **Step 3: `doc` 中说明与包运行态页分工**

---

### Task 11: 文档与演示开关

**Files:**
- Create: `doc/19-WMS与WCS包.md`
- Modify: `doc/README.md`、`doc/14-功能开关.md`、`design/README.md`（链到本计划）
- 关闭 HotStore Demo 生产默认；Stacker/FourWay 使用正式 NS

- [ ] **Step 1: 写清表前缀表、包装配示例、SUDR 语义与通讯后置说明**  
- [ ] **Step 2: 端到端演示清单（仿真 TriggerPort）**

---

## Phase H（下阶段，本计划不实施）— 通讯包

目标备忘（实现时另开计划）：

- `IEquipmentTriggerPort` 的生产实现：`SocketLciPack` / `ModbusPack` / `S7Pack`
- 多仓多活：选主 + 按连接 SingleWriter；禁止多实例同时写同一 PLC
- 报文 XML/点表配置进包；**业务仍只认语义事件**
- 可渐进替换 `InMemoryEquipmentTriggerPort`

---

## 迁移对照速查

| 来源 | 迁什么 | 不迁什么 |
|------|--------|----------|
| LES2 WMS | Stock/Location/Container；收敛后的入出库/盘点状态机；分配扣账思路 | 5+6 套单据表；Waybill 首期；设备栈；整仓 ExecuteStack |
| LES_v2 WCS | DestinationRequest 编排；SelectAisle/Location；任务树；SUDR/SUDS 语义 | LCI Socket 客户端；DeviceStack 内复制粘贴；硬编码点位 |
| RCS4Shuttle | 加权寻路思想；决策点/独享段/竖井令牌；切段任务 | LES.Route 巨石类原样；共库 ExecuteStack；UpIntfCode 甄别 |

---

## Self-Review（对照规格）

| 规格要点 | 对应 Task |
|----------|-----------|
| 单进程 + WCS 包插拔 | 1, 5, 6, 8, 9 |
| 库位级 WMS 权威 | 3, 4 |
| 薄总线拆段交接 | 5 |
| 堆垛/四向路径流量隔离 | 6 vs 8 |
| 外部 HTTP/MQ + Codec | 9 |
| 表前缀分模块 | Global + 各实体 Task |
| LES2 三单收敛 | 4 |
| LES_v2 流程、通讯后置 | 2, 6, Phase H |
| RCS4Shuttle 寻路交通 | 8 |
| 接口日志/联锁/SCADA | 7, 10 |

开放问题落地：V1 采用 1:1 单据与运输单；Stacker 首期输送+堆垛机；DestinationRequest 不自动改写后续 Leg（需运维 API 再开）。
