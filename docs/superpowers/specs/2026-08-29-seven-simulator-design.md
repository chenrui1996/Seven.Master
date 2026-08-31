# Seven.Simulator：仿真工程 → 生产上线（定稿）

状态：已定稿  
日期：2026-08-29  
决策：能力全量 Vue3 重写（方案 A）+ 闭环优先分四期（路径 1）  
参考：`D:\Junheinrich\Shuttle\RCS4Shuttle\simulation-spa`（迁能力与契约，不引程序集、不搬原生 SPA 外壳）  
配套：[`design/2026-08-29-seven-simulator-design.md`](../../../design/2026-08-29-seven-simulator-design.md)（同步摘要）、[`doc/21-仿真器与联调闭环.md`](../../../doc/21-仿真器与联调闭环.md)

---

## 0. 产品定位

**Seven.Simulator** 是独立前端工程（栈对齐 `Seven.Vue3`），面向实施与联调：

1. **编辑地图** — 工程文件 `.sevenproj.json`（可兼容导入 `.simproj.json` 核心字段）
2. **部署（生成主数据）** — Deploy 物化 `Wms_*` + 包表 `Stk_*`/`Fw_*` + 可选 `Scd_*`
3. **仿真测试（Gateway 通讯）** — 一期语义 Trigger；三期协议保真 TCP Gateway + SignalR
4. **切换生产** — Promote：去 `SIM_` 身份，写真机 IP，启用 DeviceComm/通讯包

运维业务页仍在 `Seven.Vue3`；模拟器专注 **工程化联调闭环**。

---

## 1. 四阶段产品闭环

```text
① 编辑地图
   Features 勾选 → 2D/JSON 编辑 → 保存 .sevenproj.json
        ↓ Deploy
② 部署（生成主数据）
   仓 SIM_{工程名} → Wms_Location(+Zone/Layer/Aisle)
   → Stk_ 或 Fw_ 地图种子 → 可选 Scd_View/NodeBind
   → Sim_Deployment 记录
        ↓ Start
③ 仿真测试【Gateway 通讯】
   档 A（一期）：IEquipmentTriggerPort + /api/Wcs/Triggers/*
   档 B（三期）：TCP 网关监听 + SignalR 代理 + Emulator
   配合：入库/出库快捷 → Bus → 包任务；Reset 清运行态
        ↓ Promote（禁止只改 IP）
④ 切换生产【真机 IP、通讯包】
   去 SIM_；写 Host:Port；Features.DeviceComm 或通讯包
   → Seven.Vue3 业务验收 → 上线
```

| 阶段 | 权威落点 |
|------|----------|
| 地图工程 | 本地 `.sevenproj.json`（可选后期 `Sim_Project` 云端） |
| Deploy | DB：`Wms_` + `Stk_`/`Fw_` + `Scd_` + `Sim_Deployment` |
| 仿真 | `IEquipmentTriggerPort` / 后期 Gateway Hub |
| 生产 | Promote 后的连接配置 + `Ctl_Mode`；业务在 Vue3 |

---

## 2. 架构

```text
┌──────────────────────┐     ┌─────────────────────────────┐
│  Seven.Simulator     │     │  Seven.Vue3                   │
│  Features·地图·Player │     │  单据·库存·联锁·Floor2d       │
│  Promote             │     └──────────────▲────────────────┘
└──────────┬───────────┘                    │
           │ HTTP / SignalR                 │
           ▼                                │
┌───────────────────────────────────────────┴────────────────┐
│  Seven.WebApi（同进程同库）                                  │
│  /api/simulation/*     Deploy · Undeploy · Start · Reset · Promote │
│  /api/Wcs/Triggers/*   仿真档 A                              │
│  /hubs/sim-wcs-proxy   仿真档 B（三期）                       │
│  WMS + Bus + WcsPacks + DeviceComm(生产)                     │
└────────────────────────────────────────────────────────────┘
```

### 硬规则

1. **工程文件 ≠ 运行库**：仅 Deploy 写主数据。
2. **仿真身份不可伪生产**：未 Promote 禁止把 `SIM_`/`127.0.0.1` 当生产。
3. **Promote 显式**：去仿真前缀 + 真机地址；可逆需 Undeploy/Redeploy。
4. **包隔离**：按 `packId` 写入对应包表，不合成统一路网。
5. **不引用** RCS4Shuttle / LES 程序集。

---

## 3. 工程文件模型

```json
{
  "version": 1,
  "meta": {
    "name": "Demo-Stacker",
    "features": {
      "wms": true,
      "orchestrationBus": true,
      "wcsPacks": { "stacker": true, "fourWay": false, "boxSort": false },
      "hotStore": false,
      "deviceComm": false,
      "simulator": true
    },
    "runtimeMode": "Simulation",
    "simCommsMode": "Trigger"
  },
  "map": {
    "packId": "stacker",
    "canvas": { "width": 2000, "height": 1200 },
    "layers": [{ "id": "L1", "name": "1F" }],
    "nodes": [{ "id": "...", "code": "Stk.A-01", "x": 40, "y": 40, "kind": "location" }],
    "edges": [{ "id": "...", "from": "n1", "to": "n2" }],
    "devices": [{ "id": "...", "code": "SRM01", "type": "SRM", "x": 100, "y": 100 }],
    "requestPoints": [{ "code": "Stk.RP_IN_01", "mappedLocationCode": "Stk.A-01" }],
    "connections": []
  },
  "wcsConnections": [],
  "scada": { "views": [] },
  "promote": { "devices": [] }
}
```

- **一期**：`nodes`/`edges`/`devices`/`requestPoints` 足够 Deploy。
- **二期**：补全 `connections`、设备 `space`、拓扑校验（迁自 map-editor）。
- **`.simproj.json`**：提供适配器映射到 `.sevenproj.json` 核心子集（不保证 100% 设备类型一期可用）。

---

## 4. Deploy 映射

| 工程字段 | 落库 |
|----------|------|
| `meta.name` | 仓码 `SIM_{Sanitize(name)}`，`Wms_Warehouse` |
| `map.packId` | `EnabledPackIds` + Location/`Stk`/`Fw` 前缀 |
| `map.nodes[].code` | `Wms_Location`（`PackCodeRules.EnsurePrefix`） |
| `map.edges` | stacker→`Stk_Route`；fourway→`Fw_Route`（需节点） |
| `map.requestPoints` | `Stk_RequestPoint` / `Fw_RequestPoint` |
| `map.devices`（仿真） | 记录在 Deploy JSON；连接 Host=`127.0.0.1` |
| `scada.views` | `Scd_View` + `Scd_NodeBind`（一期补齐） |
| 整包快照 | `Sim_Deployment.ProjectJson` |

Undeploy：标记 Undeployed；可选删除空占用 `SIM_` 库位。  
Reset（一期可后置到 Player）：清运行态（任务/运输单），保留主数据。

---

## 5. 仿真通讯两档

| 档 | 名称 | 一期 | 说明 |
|----|------|------|------|
| A | Trigger / 语义 Gateway | ✅ | 现有 `IEquipmentTriggerPort` + Triggers API；Player 面板 |
| B | 协议保真 Gateway | 三期 | TCP 监听 + SignalR `sim-wcs-proxy` + Emulator；对齐 RCS `shuttleSimMode=Gateway` |

`meta.simCommsMode`: `Trigger` | `Gateway`。已 Deploy 后切换档位需文档约定（建议 Redeploy 或仅允许未 Start）。

生产：`DeviceComm`（S7/Modbus）或未来通讯包实现 `IEquipmentTriggerPort`；**不是**档 B 改 IP。

---

## 6. API 清单

| API | 期 | 作用 |
|-----|----|------|
| `POST /api/simulation/projects/validate-features` | 已有 | Features 校验 |
| `POST /api/simulation/deploy` | 已有→增强 | 主数据物化 |
| `POST /api/simulation/undeploy` | 已有 | 拆除 |
| `GET /api/simulation/deployments` | 已有 | 记录 |
| `POST /api/simulation/reset` | 一期 | 清运行态 |
| `POST /api/simulation/promote` / `promote-preview` | 一期 | 去 SIM_ + 真机配置 |
| `POST /api/Wcs/Triggers/*` | 已有 | 档 A |
| `POST /api/simulation/gateway/start|stop` + Hub | 三期 | 档 B |
| Excel import | 四期 | 四向栅格等 |

开关：`Features.Simulator` 仅控前端入口；Deploy API 始终注册。

---

## 7. 前端工程结构（目标）

```text
Seven.Simulator/
  src/
    views/ Features · MapEditor · Player · Promote
    components/map/   # 二期：Canvas 编辑器模块
    components/player/# 三期：Three 场景
    lib/project/      # schema、simproj 适配、校验
    lib/comms/        # Trigger 客户端；三期 Gateway SignalR
    stores/project.ts
```

一期不加 three.js；三期再引入 `three`。

---

## 8. 四期范围

| 期 | 名称 | 交付 | 验收 |
|----|------|------|------|
| **I** | 闭环可演示 | Features 加固；JSON+简易画布；Deploy 增强（边/申请点/Scd）；Player Trigger+入库快捷；Promote API+UI；doc/21 | 堆垛 Demo：Deploy→Trigger→账本变化→Promote 预览 |
| **II** | 完整 2D 编辑器 | 设备库、端口连线、Space 编译、拓扑校验、多层 | 能画简易立库并 Deploy 成功 |
| **III** | 3D + Gateway 保真 | Three Player、本地引擎子集、TCP+SignalR+Emulator | 四向或堆垛网关报文驱动动画 |
| **IV** | 导入与运维增强 | Excel、路径组、调度看板、`.simproj` 深度兼容 | 从 RCS Demo JSON 导入可 Deploy |

非目标（全程）：在 Simulator 内重做完整 WMS CRUD；引用 LES 程序集；无 Deploy 直改生产库。

---

## 9. 决策摘要

| 项 | 选择 |
|----|------|
| 迁入方式 | A：Vue3 能力重写 |
| 节奏 | 路径 1：一期→四期顺序执行 |
| 仿真 | 先 Trigger，后 Gateway 保真 |
| 生产 | Promote + DeviceComm/通讯包 |
| 文档 | `doc/21` 为实施流程权威；本文件为规格 |
