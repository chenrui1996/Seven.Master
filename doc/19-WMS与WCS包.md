# WMS 与 WCS 包

单进程 Seven 产品 = **WMS 账本** + **薄编排总线** + **可插拔 WCS 包**（自研或外部）。按项目启用 Features 组合，例如仅堆垛机、仅四向车、堆垛机 + 外部 AGV 等。

相关开关见 [14-功能开关](./14-功能开关.md)；热层见 [15-热数据HotStore](./15-热数据HotStore.md)；PLC 通讯见 [16-设备通讯DeviceComm](./16-设备通讯DeviceComm.md)（Phase H 后置）。

---

## 1. 设计文档索引

| 文档 | 内容 |
|------|------|
| [`design/2026-08-29-wms-wcs-pack-architecture.md`](../design/2026-08-29-wms-wcs-pack-architecture.md) | 架构决策与包边界 |
| [`design/2026-08-29-wms-wcs-implementation-guide.md`](../design/2026-08-29-wms-wcs-implementation-guide.md) | 实现大纲、表前缀、主路径流程 |
| [`design/2026-08-29-seven-simulator-design.md`](../design/2026-08-29-seven-simulator-design.md) | 模拟器：Features→地图→仿真→生产 |
| [`docs/superpowers/plans/2026-08-29-wms-wcs-pack-implementation.md`](../docs/superpowers/plans/2026-08-29-wms-wcs-pack-implementation.md) | 任务级 TDD 计划 |
| [`Seven.Simulator/`](../Seven.Simulator/) | 联调 SPA（栈对齐 Vue3） |

---

## 2. 数据库表前缀

| 前缀 | 模块 | 示例表 |
|------|------|--------|
| `Wms_` | WMS 主数据与库存 | `Wms_Stock`、`Wms_Location` |
| `Bus_` | 编排总线 | `Bus_TransportOrder`、`Bus_TransportLeg` |
| `Stk_` | 堆垛机 WCS | `Stk_PutAwayTask`、`Stk_DeviceTask` |
| `Fw_` | 四向车 WCS | `Fw_ShuttleTask`、`Fw_Node` |
| `Ext_` | 外部 WCS | `Ext_System`、`Ext_MessageLog` |
| `Ifc_` | 接口日志 | `Ifc_ApiLog` |
| `Ctl_` | 联锁/运行模式 | `Ctl_Mode` |
| `Scd_` | 2D SCADA | `Scd_View`、`Scd_NodeBind` |
| `Sim_` | 仿真工程/部署元数据（可选） | `Sim_Project`、`Sim_Deployment` |

常量定义：`Seven.Domain/Wcs/TablePrefixes.cs`。库位权威只有 `Wms_Location`；包内策略表用 Code 引用，禁止第二套货位主表。

---

## 3. 功能开关矩阵

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

| 开关 | 关闭效果 |
|------|----------|
| `Wms` | WMS API 404；隐藏 WMS/SCADA 菜单；库存与三单不可用 |
| `OrchestrationBus` | 不注册总线服务；`Bus_*` 运输单不可创建 |
| `WcsPacks.Stacker` | 堆垛机包 HostedService 不启动；`Stk_*` 业务不可用 |
| `WcsPacks.FourWay` | 四向车包不启动；需 `HotStore=true` 才启用寻路/交通 |
| `WcsPacks.BoxSort` | 箱式分拣包（预留） |
| `ExternalWcs` | 外部供应商包列表（`PackId` / `Transport` / `Codec`） |

| 场景 | 建议开关 |
|------|----------|
| 纯账本/单据联调 | `Wms=true`，总线与包可先 false |
| 巷道立库仿真 | Wms + OrchestrationBus + WcsPacks.Stacker；DeviceComm 仍可 false |
| 四向车库 | + HotStore + WcsPacks.FourWay |
| 接外部供应商 | + ExternalWcs 项；MessageQueue 按传输需要 |

包开关为嵌套对象时，API/`RequiresFeature` 用显式 `IsWcsPackEnabled("Stacker")`，不要依赖仅反射顶层 bool 的 `IsEnabled`。

**生产 HotStore：** `HotStore:EnableDemoScheduler` 保持 **false**（见 [15 §3](./15-热数据HotStore.md)）；四向车业务使用 `fw:` 命名空间，禁止生产路径使用 Demo 全局 `wcs:` 键。

---

## 4. 仿真设备触发（无 PLC）

默认注册 `InMemoryEquipmentTriggerPort`（`AddSevenWcs`）。无真实 PLC 时，通过测试或运维 API 调用语义触发（如 SUDR 目的地申请），驱动堆垛机/四向车业务状态机。

Phase H 用 Socket/Modbus/S7 通讯包替换同一 `IEquipmentTriggerPort` 接口，**业务代码不改报文解析**。

---

## 5. 本地开发与验证

```powershell
cd Seven.Net8
dotnet ef database update --project Seven.Infrastructure --startup-project Seven.WebApi
dotnet run --project Seven.WebApi
dotnet test Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~Wms|FullyQualifiedName~Wcs|FullyQualifiedName~Platform|FullyQualifiedName~FourWay|FullyQualifiedName~Stacker|FullyQualifiedName~Orchestration|FullyQualifiedName~Stock|FullyQualifiedName~Order|FullyQualifiedName~Scada|FullyQualifiedName~External"
```

前端：`Seven.Vue3/src/views/Wms`、`views/Wcs/Stacker`、`views/Wcs/FourWay`、`views/Scada`；菜单按 `features.ts` 中 `WmsFolder` / `ScadaFolder` 过滤。

---

## 6. Phase H（下阶段，当前未实现）

- 通讯包：Socket / Modbus / S7，替换 `IEquipmentTriggerPort` 实现
- 多活调度写者
- 不阻塞当前 WMS/总线/包骨架联调

---

## 7. 上线检查清单

- [ ] 表前缀无混用；迁移在 `Seven.Infrastructure/Migrations/`
- [ ] 仅启用本项目需要的 WCS 包
- [ ] 多包时交接位与 HandoverLink 已配置
- [ ] `HotStore:EnableDemoScheduler=false`
- [ ] TriggerPort 为预期实现（InMemory 仿真或通讯包）
- [ ] 接口日志（`Ifc_*`）与联锁页（`Ctl_*`）可访问
