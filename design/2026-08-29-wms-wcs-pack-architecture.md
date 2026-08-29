# 立库合并 WMS/WCS 架构：可插拔 WCS 包

状态：已确认（实现计划已出）  
日期：2026-08-29  
范围：单进程产品内 WMS 与多套自研/外部 WCS 的组合架构  
实现计划：`docs/superpowers/plans/2026-08-29-wms-wcs-pack-implementation.md`  

对齐现有能力：

- `ICacheService` / 实体缓存大纲 — `design/entity-cache-implementation-outline.md`
- `IHotStore` — `doc/15-热数据HotStore.md`
- DeviceComm — `doc/16-设备通讯DeviceComm.md`
- 告警 / MQ — `doc/09-告警模块.md`、`doc/10-消息队列指南.md`

---

## 0. 决策摘要

| 决策 | 选择 |
|------|------|
| 交付形态 | 单进程：WMS + 已启用的 WCS 包同进程 |
| 外部库区账本 | 我方 WMS 管到库位级；外部系统只执行与回传 |
| 跨区/跨包任务 | 薄编排总线统一拆段、衔接与补偿 |
| 外部接入 | HTTP 与 MQ 平台均支持；**同一外部系统只用一种**；按供应商 Codec 映射报文 |
| 自研 WCS | 以 **WCS 包** 为单位插拔（堆垛机包、四向车包、箱式分拣包等） |
| 包内差异 | 各包调度、路径、流量模型独立；禁止合成统一路网 |
| 仿真与上线 | 独立 **Seven.Simulator**（栈对齐 Vue3）：Features → 地图 Deploy → 仿真 → Promote 生产；见 `design/2026-08-29-seven-simulator-design.md` |

---

## 1. 目标与非目标

### 1.1 目标

- 合并传统「独立 WMS + 独立 WCS」为 **一套产品进程**，共享库位级库存账本。
- **堆垛机 WCS**（堆垛机 + 输送线 + RGV 等）、**四向车 WCS**（四向车 + 输送线 + 提升机等）、**箱式分拣** 等自研系统可 **独立存在、按项目组合**。
- 外部供应商系统（STU、AGV、箱式线、四向车供应商 WCS 等）以 **外部包 + 适配器** 接入；可与自研包并存。
- 设备族可从外部包 **迁移** 为自研包，而不改 WMS 账本与 TransportOrder 模型。

### 1.2 非目标（首期）

- 多实例同时写 PLC / 多活调度写者。
- 跨包自动合成一张统一路径/流量图。
- 用 `ICacheService` 承载调度占道或设备任务队列。
- 规定某一供应商的具体报文格式（仅规定适配边界）。

---

## 2. 总体分层

```text
┌──────────────────────────────────────────────────────────┐
│  接入层                                                   │
│  ERP/MES · Seven.Vue3（运维） · Seven.Simulator（联调）   │
│  SignalR 看板                                             │
├──────────────────────────────────────────────────────────┤
│  WMS 域（项目常驻）                                        │
│  主数据 · 库位级库存 · 上下架/波次/分配 · 业务任务          │
│  DB 权威 + ICacheService 通道 A/B                          │
├──────────────────────────────────────────────────────────┤
│  编排总线 Orchestration Bus（薄）                           │
│  TransportOrder 跨包拆段 · 包间交接 · 补偿 · 回写账本触发    │
├──────────────────────┬───────────────────────────────────┤
│  WCS 包（可插拔）     │  仿真 / 生产通讯面                  │
│  Stacker / FourWay…  │  Simulation: TriggerPort / Sim_    │
│  External Adapter    │  Production: DeviceComm / 通讯包   │
│                      │  Promote 切换（Simulator 驱动）     │
└──────────────────────┴───────────────────────────────────┘
```

| 层 | 做什么 | 不做什么 |
|----|--------|----------|
| **Seven.Simulator** | Features 工程、地图 Deploy、仿真节拍、Promote | 不替代 Vue3 日常单据 CRUD |
| **WMS 域** | 账本、分配、业务任务状态 | 不直连 PLC；不解析供应商报文；不跑包内寻路 |
| **编排总线** | 拆段、激活下一 Leg、交接、失败策略、触发落账 | 不共享/合并各包路网 |
| **自研 WCS 包** | 包内设备集、调度、路径/流量、通讯 | 不拥有全局库存扣减权威 |
| **外部 WCS 包** | 传输 + Codec + 回调归一 | 不拥有库位账本；不跨系统替总线拆段 |

### 2.1 硬边界

1. **WCS 包是插拔单位**：路径、流量、调度实现随包隔离；平台只复用 DB、HotStore 引擎、DeviceComm、告警等能力。
2. **包间只经总线**：跨包搬运经交接位衔接；禁止包 A 直接改包 B 的 HotStore。
3. **账本只在 WMS**：外部/包内回传位置与完成态 → 总线推进 → WMS 在约定里程碑落账。
4. **内外是部署身份**：路由键 = `PackId`（含 `ext:…`）；同一设备族可从外部包迁到自研包。
5. **同名设备分域**：例如「输送线」在 Stacker 包与 FourWay 包是不同设备域，配置与节拍不混用。
6. **仿真与生产分离**：工程态在 Simulator；Deploy 写库；Promote 前禁止用仿真身份冒充真机。

### 2.2 项目装配示例

| 项目 | 启用包 |
|------|--------|
| 仅巷道立库 | `StackerWCS` |
| 仅四向车库 | `FourWayWCS` |
| 四向车 + 箱式分拣 | `FourWayWCS` + `BoxSortWCS` |
| 巷道 + 外部 AGV | `StackerWCS` + `ext:agv:…` |
| 先外后内（四向车） | 初期 `ext:fourway:…` → 后期改 `FourWayWCS` |

---

## 3. 统一任务模型

### 3.1 三层单据

| 层级 | 名称 | 创建方 | 职责 |
|------|------|--------|------|
| L1 | 业务任务（上架/下架/移库等） | WMS | 库存意图、分配；不写设备细节 |
| L2 | TransportOrder（运输单） | 编排总线 | 「容器从 A 到 B」；可跨多个包 |
| L3 | Leg / PackTask（执行段） | 总线拆段后交目标包 | 仅在一个包内执行；包内再拆设备任务 |

```text
业务任务 (WMS)
    → TransportOrder (总线)
        → Leg#1 @ StackerWCS
        → Leg#2 @ ext:agv:vendorA
        → Leg#3 @ FourWayWCS
    → 全部 Leg 成功后回写业务任务 / 库存
```

### 3.2 TransportOrder / Leg（逻辑字段）

**TransportOrder**

- `OrderId`、容器、库存/物料引用  
- `FromLocation` / `ToLocation`（WMS 库位）  
- `Legs[]`、整单状态：`Created → Planning → Executing → Completed | Failed | Cancelling`

**Leg**

- `LegId`、`Seq`、`PackId`  
- `HandoverIn` / `HandoverOut`（WMS 交接库位；单包项目可与起终点相同或省略中间交接）  
- 状态由包上报，总线归一：`Accepted | Running | Completed | Failed | …`

### 3.3 包间交接

- 总线只认 **交接位**（WMS 库位，接口区主数据）。  
- `Leg[n]` 完成且容器已在约定交出位 → 才激活 `Leg[n+1]`。  
- 下一包从交接位起做 **本包** 路径规划；不继承上一包路网状态。

### 3.4 包内自治

| 包 | 包内要点（示例） |
|----|------------------|
| Stacker | 巷道队列、堆垛机串行、输送/RGV 子任务；可不建四向车式路网流量 |
| FourWay | 本包路网寻路 + 流量占道（独立 HotStore NS） |
| BoxSort | 分拣线/合流等本包模型 |
| External | Adapter 将 Leg 映射为供应商任务；回调推进 Leg |

包内状态机细节不泄漏到总线；总线只消费归一事件（含 `DestinationRequest` 等扩展事件，由 Codec/包翻译）。

### 3.5 幂等与补偿

- 下发与回调幂等键：`LegId`（及供应商侧映射键）。  
- Leg 失败：总线暂停后续 Leg；默认 **不** 自动跨包重路由。  
- 取消：总线自上而下；包内停设备或发供应商取消。  
- 外部超时：按配置 `QueryLeg` 对账；仍无结果则 Failed + 告警。  
- 交接位卡住：告警 + 运维 API 强制完成/取消。

---

## 4. WCS 包契约与装配

### 4.1 `IWcsPack`

| 能力 | 说明 |
|------|------|
| `PackId` / `Kind` | 如 `stacker`、`fourway`、`boxsort`、`ext:agv:{vendor}` |
| `CanHandle(from, to)` | 是否覆盖两点间本包路径（供总线规划） |
| `AcceptLegAsync` | 接单 |
| `CancelLegAsync` | 取消 |
| `QueryLegAsync` | 查询/对账 |
| 事件上报 | Progress / Completed / Failed / DestinationRequest… → 总线 |
| `Health` | 联机、积压、可否接单 |

包内调度器、路径/流量、点位、Codec **不进入** 此契约。

### 4.2 外部包

```text
ExternalWcsPack
  ├── IExternalTransport   （Http 或 Mq，按系统实例二选一）
  ├── IVendorCodec         （报文 ↔ Leg，按供应商）
  └── 回调入口             （HTTP 或 MQ Consumer）
         → 归一为 Leg 事件 → 总线
```

规则：

- 平台同时具备 HTTP 与 MQ；**同一外部系统只配一种传输**。  
- 同项目可混用多种传输（不同外部系统）。  
- 同种传输下不同供应商用不同 Codec。  
- 新供应商 = 新 Codec + 选 Transport + 注册 PackId。

### 4.3 功能开关（示意）

```json
"Features": {
  "Wms": true,
  "OrchestrationBus": true,
  "WcsPacks": {
    "Stacker": true,
    "FourWay": false,
    "BoxSort": false
  }
},
"ExternalWcs": [
  {
    "PackId": "ext:agv:vendorA",
    "Transport": "Http",
    "Codec": "VendorAAgv",
    "BaseUrl": "https://…"
  }
]
```

| 规则 | 含义 |
|------|------|
| 未启用包 | 不注册 HostedService、不占 HotStore NS、隐藏菜单/API |
| 单包项目 | 总线仍在；TransportOrder 常为单段 |
| 多包 | 启动校验交接位主数据；缺失则拒绝跨包单 |
| SingleWriter | 各包通讯/调度默认单活（对齐 DeviceComm） |

### 4.4 代码落位（建议）

| 位置 | 职责 |
|------|------|
| `Seven.Application` | `IWcsPack`、`IOrchestrationBus`、Order/Leg 契约 |
| `Seven.Infrastructure/Wcs/Bus/` | 总线、规划、衔接 |
| `Seven.Infrastructure/Wcs/Packs/Stacker\|FourWay\|BoxSort/` | 自研包 |
| `Seven.Infrastructure/Wcs/External/` | Transport、Codec、注册与回调 |
| 功能开关 | 对齐现有 `Features.*` / `RequiresFeature` 模式 |

---

## 5. 数据、热路径与观测

### 5.1 数据归属

| 数据 | 权威 | 通道 |
|------|------|------|
| 库存/库位/容器主数据 | WMS · DB | A/B（实体缓存大纲） |
| 业务任务 | WMS · DB | A |
| TransportOrder / Leg | 总线 · DB | A；派发与完成读 DB |
| 包内设备任务、地图冷配置 | 各包 · DB | 包内约定 |
| 包内路径/流量/车态 | 各包 HotStore NS | C；禁止跨包读写 |
| 外部报文流水 | 外部包 | 审计与对账 |

### 5.2 热路径

- 调度节拍在 **包内** HostedService；总线只做 Leg 激活与衔接。  
- HotStore Key 强制带 `PackId` 前缀；预热/落库按包注册。  
- 能力按包声明：四向车包装路网流量；堆垛机包可不启用同类模型。

### 5.3 进程恢复

- Order/Leg 从 DB 恢复。  
- 各包 HotStore 预热完成前不接新单（对齐 HotStore `IsReady` 思路）。

### 5.4 观测

| 手段 | 约定 |
|------|------|
| 告警码 | `WMS*` / `BUS*` / `WCS.{PackId}*` / `DEV*`；外部可走 MQ `RaiseAlarmCommand` |
| 指标 | 按包 accept/complete/fail/lag；总线 order_open、leg_handover_wait |
| 健康检查 | 总线 + 已启用包 Health；未启用包不注册 |
| UI | WMS 全局；各包运行态页随 Features 显示 |

---

## 6. 与既有设计的关系

| 既有文档/能力 | 关系 |
|---------------|------|
| 实体缓存大纲 | WMS 账本与业务任务通道不变；设备执行任务落在包内 + Leg |
| HotStore | 引擎复用；**命名空间与预热按包拆分**，禁止全局一张路网 |
| DeviceComm | 自研包内部 PLC 通讯；不用于外部 HTTP/MQ |
| Alarm / MQ | 外部状态与告警入站；不替代总线任务模型 |
| LES2 式 WMS↔WCS HTTP | 收敛为 **外部包** 的一种 Transport+Codec，不再作为唯一集成形态 |

---

## 7. 演进顺序（建议）

1. **总线骨架** + TransportOrder/Leg + 单包（Stacker）打通入库/出库。  
2. **外部包框架**（HttpTransport + 一个真实供应商 Codec）。  
3. **FourWay 自研包**（独立路径/流量 HotStore NS）。  
4. **BoxSort** 及其他包按项目插入。  
5. 多包交接位主数据与运维补偿 API 完善。

---

## 8. 术语表

| 术语 | 含义 |
|------|------|
| WCS 包 | 可独立启用的执行子系统（自研或外部） |
| 编排总线 | 跨包拆段与交接的薄层，不含包内寻路 |
| TransportOrder | 总线运输单 |
| Leg | 运输单在某一包上的执行段 |
| 交接位 | WMS 库位，包间唯一约定接口 |
| Codec | 供应商报文与 Leg 事件的双向映射 |
| HotStore NS | 按 PackId 隔离的热数据前缀空间 |

---

## 9. 开放问题（实现期再定，不阻塞本架构）

1. 交接位占用超时的默认 SLA 与人工策略矩阵。  
2. 业务任务与 TransportOrder 1:1 还是 1:N（波次拆单）。  
3. 各自研包首期设备子集（如 Stacker 是否首期含 RGV）。  
4. 外部 `DestinationRequest` 是否允许总线改写后续 Leg 终点（需严格权限与审计）。
