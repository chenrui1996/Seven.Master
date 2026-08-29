# WMS / WCS 包：实现大纲、结构、流程与开发使用

状态：与实现计划同步（分支 `feature/wms-wcs-pack` 工作区已落地 Task 1–11 骨架 + 入库→堆垛机仿真 E2E；未 commit）  
日期：2026-08-29  
架构规格：[`2026-08-29-wms-wcs-pack-architecture.md`](./2026-08-29-wms-wcs-pack-architecture.md)  
实现计划：[`../docs/superpowers/plans/2026-08-29-wms-wcs-pack-implementation.md`](../docs/superpowers/plans/2026-08-29-wms-wcs-pack-implementation.md)

本文给开发与实施人员：**做什么、落在哪、怎么跑通、怎么开关与扩展**。不替代任务级计划中的逐步 TDD 清单。

---

## 1. 实现大纲

### 1.1 产品形态

单进程 Seven 产品 = **WMS 账本** + **薄编排总线** + **可插拔 WCS 包**（自研或外部）。  
项目通过 Features 装配组合，例如仅堆垛机、仅四向车、堆垛机+外部 AGV、四向车+箱式分拣等。

### 1.2 分阶段交付（与计划 Phase 对齐）

> **实现状态（2026-08-29）：** Task 1–10 已脚手架落地（Features/契约、WMS 主数据与三单、编排总线、堆垛机包、Ifc/Ctl、四向车包、外部包、2D SCADA）。Phase H（通讯包 Socket/Modbus/S7、多活）留待下阶段。

| 阶段 | 交付物 | 验收要点 |
|------|--------|----------|
| **A** | Features、表前缀、`IWcsPack` / 总线 / `IEquipmentTriggerPort` | 开关可关；仿真触发可测 |
| **B** | `Wms_` 主数据+库存；**一种**入库/出库/盘点单 | 账本守恒；无多套同构单据表 |
| **C** | `Bus_TransportOrder` / `Leg` | 单段与跨包交接；失败暂停后续 |
| **D** | `Stk_` 堆垛机包（SUDR/SUDS **语义**） | 申请→巷道/货位→下发；与四向隔离 |
| **E** | `Ifc_` 接口日志、`Ctl_` 联锁/模式 | 拒单、审计可查 |
| **F** | `Fw_` 四向车包（寻路+交通 HotStore） | 小图寻路/对向冲突单测；不碰 `Stk_` |
| **G** | `Ext_` 外部 Http\|Mq + Codec | Fake 往返；新供应商可插 |
| **H** | 通讯包 Socket/Modbus/S7、多活 | **下阶段**；只替换 TriggerPort 实现 |
| **I** | `Scd_` 2D SCADA | 只读库位/任务态 |
| **S0–S6** | **Seven.Simulator**（Features→地图→仿真→Promote） | 见 [`2026-08-29-seven-simulator-design.md`](./2026-08-29-seven-simulator-design.md) |

### 1.3 迁移来源（只迁逻辑，不引程序集）

| 来源 | 迁入 | 明确不迁 |
|------|------|----------|
| LES2 WMS | Stock/Location/Container；收敛三单状态机 | 多套入出库表；Waybill 首期 |
| LES_v2 堆垛 WCS | 目的地申请编排；巷道/货位分配；任务树 | LCI Socket；DeviceStack 复制粘贴 |
| RCS4Shuttle | 加权寻路、决策点/独享段思想 | LES.Route 巨石；共库 ExecuteStack |

### 1.4 本阶段不做

- 多仓多活同时写 PLC  
- 跨包合成统一路网  
- 用 `ICacheService` 做占道/设备队列  
- 绑定某供应商具体报文（仅 Codec 边界）

---

## 2. 系统结构

### 2.1 逻辑分层

```text
接入：ERP/MES · Seven.Vue3 · Seven.Simulator · SignalR · 2D SCADA
                │
         ┌──────▼──────┐
         │   WMS 域     │  Wms_* 库存/三单（库位权威）
         └──────┬──────┘
         ┌──────▼──────┐
         │ 编排总线 Bus │  Bus_* TransportOrder / Leg
         └──────┬──────┘
    ┌───────────┼───────────┬────────────┐
    ▼           ▼           ▼            ▼
 Stacker     FourWay     BoxSort      External
 Stk_*       Fw_*        （预留）      Ext_* + Codec
    │           │
    └─────┬─────┘
          ▼
 IEquipmentTriggerPort
    │              │
 仿真(Simulator)  生产(通讯包 / DeviceComm)
```

### 2.2 代码落位（Seven.Master）

| 层 | 路径 |
|----|------|
| 契约 | `Seven.Net8/Seven.Application/Wcs/`、`…/Wms/` |
| 实体 | `Seven.Net8/Seven.Domain/Entities/Wms|Bus|Wcs/Stacker|Wcs/FourWay|Platform/` |
| 表前缀常量 | `Seven.Net8/Seven.Domain/Wcs/TablePrefixes.cs`（含未来 `Sim_`） |
| 实现 | `Seven.Net8/Seven.Infrastructure/Wms/`、`…/Wcs/Bus|Packs|External|Triggers/` |
| DI | `AddSevenWcs` ← `DependencyInjection.AddSevenInfrastructure` |
| API | `Seven.Net8/Seven.WebApi/Controllers/Wms|Wcs/`；仿真 `/api/simulation/*` |
| 运维 UI | `Seven.Vue3/src/views/Wms|Wcs|Scada/` |
| **联调 UI** | **`Seven.Simulator/`**（独立 Vite 工程） |
| 测试 | `Seven.Net8/Seven.Tests/Wms|Wcs/` |

### 2.3 数据库表前缀（强制）

| 前缀 | 模块 |
|------|------|
| `Wms_` | WMS |
| `Bus_` | 编排总线 |
| `Stk_` | 堆垛机 WCS |
| `Fw_` | 四向车 WCS |
| `Ext_` | 外部 WCS |
| `Ifc_` | 接口日志 |
| `Ctl_` | 联锁/运行模式 |
| `Scd_` | 2D SCADA |
| `Sim_` | 仿真工程/部署元数据（可选） |

Fluent：`ToTable(TablePrefixes.Wms + "Stock")` → `Wms_Stock`。  
库位主数据只有 `Wms_Location`；包内策略表用 Code 引用，禁止第二套货位主表。

### 2.4 HotStore 命名空间

| 前缀 | 用途 |
|------|------|
| `stk:` | 堆垛机忙闲/包内热态（按需） |
| `fw:` | 四向车边占用、车态 |
| `bus:` | 总线侧可选热镜像 |

禁止生产路径继续使用 Demo 全局 `wcs:` 键。

---

## 3. 实现流程（主路径）

### 3.1 入库（堆垛机项目示例）

```text
创建/审核 Wms_InboundOrder
  → 组盘：Container + Stock@收货位
  → Bus.CreateTransportOrder(from, to)     // 与业务单 1:1
  → 规划 Leg（单包则一段 @stacker）
  → Stacker.AcceptLeg → Stk_PutAwayTask / DeviceTask
  → [现场] DestinationRequested(SUDR 语义)
       → Stk 巷道分配 → 货位分配（仅 Stk_ 策略）
       → DispatchDestination(SUDS 语义) / DispatchMove(SUM*)
  → SegmentFeedback → 完成 DeviceTask → Leg Completed
  → 总线收齐 → WMS 落账（库存到目标库位）
```

### 3.2 出库

```text
审核 Wms_OutboundOrder → 库存预留
  → TransportOrder → Pack Leg（取货/输送）
  → 语义触发与任务树（同包内状态机）
  → 完成 → 扣账/发运
```

### 3.3 跨包

```text
Leg[n] @ PackA 完成且容器在交接位(Wms_Location.IsHandover)
  → 激活 Leg[n+1] @ PackB（PackB 自有路径/流量，不继承 PackA 路网）
```

无 `Wms_HandoverLink` 配置则 Planning 失败。

### 3.4 盘点

```text
Wms_CycleCount 计划 → 实盘 → 差异确认 → 调账回写 Wms_Stock
```

（可与下架运输单组合；首期允许纯账面盘点。）

### 3.5 推荐开发顺序

1. Task 1–2：开关 + 契约 + InMemory TriggerPort  
2. Task 3–4：WMS 主数据与三单  
3. Task 5：总线  
4. Task 6：堆垛机包 + 端到端仿真  
5. Task 7：Ifc/Ctl  
6. Task 8–10：四向 / 外部 / SCADA  
7. Phase H：通讯包替换 TriggerPort  

---

## 4. 要点与硬规则

1. **包是插拔单位**：调度、路径、流量实现不跨包复用；平台只复用 DB、HotStore 引擎、DeviceComm（后置）、告警。  
2. **堆垛 ≠ 四向**：`SelectAisle`/`SelectLocation` 只在 `Stk_`；四向寻路/Grant 只在 `Fw_`。  
3. **账本只在 WMS**：包与外部只回报位置/完成，由总线触发落账。  
4. **通讯后置**：业务只依赖 `IEquipmentTriggerPort`；SUDR/SUDS 是语义名，不是 Socket 实现。  
5. **外部一系统一种传输**：Http 或 Mq；Codec 按供应商；平台两种都具备。  
6. **SingleWriter**：各包调度/未来通讯默认单活；多活属 Phase H。  
7. **EF 迁移**只写入 `Seven.Infrastructure/Migrations/`。  
8. **不引用** LES2 / LES_v2 / RCS4Shuttle 工程。

---

## 5. 开发与使用方式

### 5.1 本地开发（后端）

```powershell
cd Seven.Master/Seven.Net8
# 按需改 Seven.WebApi/appsettings.Development.json 中 Features
dotnet ef database update --project Seven.Infrastructure --startup-project Seven.WebApi
dotnet run --project Seven.WebApi
dotnet test Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~Wms|FullyQualifiedName~Wcs"
```

### 5.2 功能开关（示意）

```json
"Features": {
  "Wms": true,
  "OrchestrationBus": true,
  "WcsPacks": {
    "Stacker": true,
    "FourWay": false,
    "BoxSort": false
  },
  "HotStore": true,
  "DeviceComm": false,
  "Alarm": true,
  "MessageQueue": false
},
"ExternalWcs": []
```

| 场景 | 建议开关 |
|------|----------|
| 纯账本/单据联调 | Wms=true，总线/包可先 false |
| 巷道立库仿真 | Wms + OrchestrationBus + WcsPacks.Stacker；DeviceComm 仍可 false（InMemory 触发） |
| 四向车库 | + HotStore + WcsPacks.FourWay |
| 接供应商 | + ExternalWcs 项；MessageQueue 按传输需要 |

包开关为嵌套对象时，API/`RequiresFeature` 用显式 `IsWcsPackEnabled("Stacker")`，不要依赖仅反射顶层 bool 的 `IsEnabled`。

### 5.3 仿真设备触发（无 PLC）

- 默认注册 `InMemoryEquipmentTriggerPort`。  
- 测试或运维 API 调用 `SimulateDestinationRequestAsync`（SUDR）等，驱动 Stacker/FourWay 业务。  
- Phase H 用真实通讯包替换同一接口，**业务代码不改报文解析**。

### 5.4 新增自研 WCS 包

1. `FeatureOptions.WcsPacks` 增加布尔（如 `BoxSort`）。  
2. `Domain/Entities/Wcs/YourPack/` + 表前缀（新前缀需写入 `TablePrefixes` 与架构约定）。  
3. 实现 `IWcsPack`，放 `Infrastructure/Wcs/Packs/...`。  
4. `AddSevenWcs` 内按开关注册 HostedService。  
5. Vue `features.ts` + 菜单种子；交接位主数据若跨包则配置 `Wms_HandoverLink`。

### 5.5 新增外部供应商

1. 实现 `IVendorCodec` + 选择 `IExternalTransport`（Http 或 Mq）。  
2. `ExternalWcs` 配置 `PackId` / `Transport` / `Codec` / 连接串。  
3. 回调入口归一为 `LegEvent` → 总线。  
4. 报文落 `Ext_MessageLog` / `Ifc_ApiLog`。

### 5.6 前端

- `stores/features.ts` 拉 `/api/config/features`，按 `menuFeatureMap` 滤菜单。  
- 页面目录：`views/Wms`、`views/Wcs/Stacker`、`views/Wcs/FourWay`、`views/Scada`。  
- 实时：Alarm / 包运行态 / SCADA 依赖 SignalR（与现有 Features.SignalR 一致）。

### 5.7 联锁与运维

- `Ctl_Mode`：自动 / 半自动 / 手动；急停时 `IWcsPack.Health` 不可接单。  
- Leg 卡交接位：运维 API 强制完成/取消（默认不自动跨包改道）。  
- 告警码约定：`WMS*` / `BUS*` / `WCS.{packId}*` / `DEV*`。

### 5.8 实施检查清单（上线前）

- [ ] 表前缀无混用；迁移目录正确  
- [ ] 仅启用本项目需要的包  
- [ ] 交接位与 HandoverLink 已配置（多包时）  
- [ ] Stacker 策略（AssignmentPolicy）与四向地图版本已导入  
- [ ] 生产关闭 HotStore Demo；TriggerPort 为预期实现（仿真或通讯包）  
- [ ] 接口日志与联锁页可访问  
- [ ] 菜单种子已跑（仓储WMS / 立库WCS / 执行运维）；角色已授 Auth  
- [ ] 出库有 ToLocation 时勿手动 Ship（等运输完成自动扣账）  

### 5.9 出库与运输（补齐说明）

| 场景 | 行为 |
|------|------|
| 无总线 / 无 ToLocation | `Approve` → `Ship` 立即扣账 |
| 有总线且 From≠To | `Approve` 预留 Available + 建运输单，状态 Executing；**禁止**手动 Ship；运输完成后 handler 移库并完成出库单 |

### 5.10 Seven.Simulator 使用（联调闭环）

完整设计见 [`2026-08-29-seven-simulator-design.md`](./2026-08-29-seven-simulator-design.md)。

```powershell
cd Seven.Master/Seven.Simulator
npm install
npm run dev
```

1. **Features** — 勾选本项目 WMS/总线/WCS 包，写入工程 `meta.features`  
2. **地图** — 导入或绘制 → Deploy 到库  
3. **仿真** — 单机触发 / 流程单据 / 调度观察  
4. **生产** — Promote，填写真机地址，再用 `Seven.Vue3` 业务验收  

与 Vue3 分工：模拟器管工程与联调；Vue3 管日常运维。 

---

## 6. 相关文档索引

| 文档 | 内容 |
|------|------|
| `design/2026-08-29-wms-wcs-pack-architecture.md` | 架构决策与边界 |
| `design/entity-cache-implementation-outline.md` | 库存/任务缓存通道 |
| `docs/superpowers/plans/2026-08-29-wms-wcs-pack-implementation.md` | 任务级实现计划 |
| `doc/14-功能开关.md` | Features 总述（实现后补 WMS/WCS） |
| `doc/15-热数据HotStore.md` / `doc/16-设备通讯DeviceComm.md` | 热层与 PLC（通讯包阶段） |
| `doc/19-WMS与WCS包.md` | 产品手册（实现后撰写） |
