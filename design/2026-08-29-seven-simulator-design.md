# Seven.Simulator：仿真工程 → 生产上线

状态：草案（与 WMS/WCS 包架构配套）  
日期：2026-08-29  
参考：`D:\Junheinrich\Shuttle\RCS4Shuttle\simulation-spa`（概念复用，不引程序集）  
配套：[`2026-08-29-wms-wcs-pack-architecture.md`](./2026-08-29-wms-wcs-pack-architecture.md)、[`2026-08-29-wms-wcs-implementation-guide.md`](./2026-08-29-wms-wcs-implementation-guide.md)

---

## 0. 产品定位

**Seven.Simulator** 是独立前端工程（与 `Seven.Vue3` 同技术栈），面向实施与联调：

1. **选 Features 并构建** — 声明本项目启用的 WMS / 总线 / WCS 包组合  
2. **导入或绘制地图** — 工程文件（`.sevenproj.json`）与画布编辑  
3. **仿真测试** — 单机、流程、调度（对接 `IEquipmentTriggerPort` / 包内仿真身份）  
4. **切换生产** — 填写真机地址，Promote 后上线  

运维业务页仍在 `Seven.Vue3`；模拟器专注 **工程化联调闭环**，不替代账本/单据 UI。

---

## 1. 期望工作流（四阶段）

```text
① Features 构建
   勾选 Wms / OrchestrationBus / WcsPacks.* / HotStore / DeviceComm…
   → 校验组合（如仅 Stacker；FourWay+HotStore）
   → 生成/更新 appsettings 片段与工程 meta.features
        ↓
② 地图导入 / 绘制
   Excel/JSON 导入 或 2D 拓扑编辑
   → 保存本地工程 .sevenproj.json
   → Deploy：物化 Wms_Location / 包内地图(Stk_|Fw_) / Scd_ 绑定
        ↓
③ 仿真测试
   模式 = Simulation
   · 单机：仿真触发 SUDR/SUDS/段反馈；设备忙闲
   · 流程：入库/出库单据 → 总线 → 包
   · 调度：四向寻路/占边；堆垛巷道分配
   → Reset 可清运行态、保留主数据
        ↓
④ 生产模式
   Promote：去仿真前缀 / 写入真机 Host:Port
   · DeviceComm 或通讯包接真 PLC
   · 禁止仿真身份下只改 IP 当生产
   → 真机联调 → 上线
```

| 阶段 | 谁做 | 权威落点 |
|------|------|----------|
| Features | 模拟器 UI + 可选写入 WebApi 配置建议 | 工程 `meta`；生产以 `appsettings` / 环境变量为准 |
| 地图 | 模拟器编辑；Deploy API | DB：`Wms_` + 包表 + `Scd_` |
| 仿真 | 模拟器播放 + WebApi 仿真栈 | `IEquipmentTriggerPort` / `Sim_` 设备身份 |
| 生产 | Promote API + 运维填地址 | 真机连接；`Ctl_Mode` |

---

## 2. 架构落位

```text
┌─────────────────────┐     ┌──────────────────────────────┐
│  Seven.Simulator    │     │  Seven.Vue3（运维/业务）       │
│  Vue3+Vite+Pinia    │     │  单据 / 库存 / 联锁 / SCADA只读 │
│  Element Plus       │     └──────────────▲───────────────┘
│  地图编辑·播放·Promote│                    │
└──────────┬──────────┘                    │
           │ HTTP / SignalR                │
           ▼                               │
┌──────────────────────────────────────────┴───────────────┐
│  Seven.WebApi                                              │
│  /api/simulation/*   Features.Simulator                    │
│  Deploy / Undeploy / Start / Reset / Promote               │
│  与 WMS/Bus/Packs 共用同一进程与库                           │
│  IEquipmentTriggerPort ← 仿真；Phase H 真通讯包 ← 生产       │
└────────────────────────────────────────────────────────────┘
```

### 2.1 与现有分层关系

| 组件 | 仿真期 | 生产期 |
|------|--------|--------|
| WMS / Bus / WcsPack | 同一实现 | 同一实现 |
| `IEquipmentTriggerPort` | InMemory / SimBridge（模拟器驱动） | 通讯包或 DeviceComm |
| 设备连接配置 | `SIM_` 前缀身份，Host=127.0.0.1 或仿真回环 | Promote 后真机地址 |
| `Scd_` | Deploy 可生成绑定 | 运维微调 |
| HotStore | 四向仿真必开 | 同左 |

### 2.2 硬规则

1. **工程文件 ≠ 运行库**：`.sevenproj.json` 是设计态；Deploy 才写库。  
2. **仿真身份不可伪生产**：未 Promote 不得把 `SIM_` 设备 IP 改成真机冒充上线。  
3. **Promote 可逆需 Undeploy/Redeploy**：禁止半仿真半真机混用同一物理设备。  
4. **包隔离不变**：地图 Deploy 按 Features 写入 `Stk_` 或 `Fw_`，不合成统一路网。  
5. **表前缀**：仿真工程元数据可用 `Sim_`（如 `Sim_Project`、`Sim_Deployment`）；业务表前缀不变。

---

## 3. 工程文件模型（概念）

```json
{
  "version": 1,
  "meta": {
    "name": "Demo-Stacker",
    "features": {
      "wms": true,
      "orchestrationBus": true,
      "wcsPacks": { "stacker": true, "fourWay": false },
      "hotStore": false,
      "deviceComm": false
    },
    "runtimeMode": "Simulation"
  },
  "map": {
    "packId": "stacker",
    "nodes": [],
    "edges": [],
    "devices": [],
    "requestPoints": []
  },
  "dispatch": { "scripts": [] },
  "scada": { "views": [] }
}
```

- **导入**：Excel/CSV/JSON（四向栅格优先参考 RCS `SimExcelMapImport` 思路）  
- **绘制**：2D 拓扑（节点/边/设备/申请点）；首期不做 Three.js（可二期）

---

## 4. 后端 API（建议）

| API | 作用 |
|-----|------|
| `POST /api/simulation/projects/validate-features` | 校验 Features 组合 |
| `POST /api/simulation/deploy` | 工程 → DB 主数据 + 仿真设备身份 |
| `POST /api/simulation/undeploy` | 拆除仿真部署（保留或清空可配置） |
| `POST /api/simulation/start` / `stop` / `reset` | 运行态 |
| `POST /api/simulation/promote` | 去仿真前缀，写真机地址 |
| 既有 `POST /api/Wcs/Triggers/*` | 单机语义仿真（过渡期） |

开关：`Features.Simulator`（默认 false）。表：`Sim_Project`（可选云端存工程）、`Sim_Deployment`（部署记录）。

---

## 5. Seven.Simulator 工程结构

```text
Seven.Master/
  Seven.Vue3/          # 业务运维
  Seven.Simulator/     # 本工程（新增）
    package.json       # 与 Vue3 对齐的 vue/vite/pinia/element-plus/signalr
    src/
      views/
        Features.vue   # ① 选 Features
        MapEditor.vue  # ② 导入/绘制
        Player.vue     # ③ 仿真播放与测试
        Promote.vue    # ④ 生产接入
      stores/project.ts
      api/http.ts
    vite.config.ts     # 开发代理 → WebApi
```

**技术栈对齐（与 Seven.Vue3）：** Vue 3.5 · Vite 8 · TypeScript · Pinia · Element Plus · vue-router · axios · `@microsoft/signalr`（可选 echarts）。首期 **不加** three.js。

---

## 6. 实现大纲（分阶段）

| Phase | 内容 | 依赖 |
|-------|------|------|
| **S0** | 脚手架 Seven.Simulator + 四页路由骨架 + 代理 WebApi | 无 |
| **S1** | Features 页：勾选、校验、导出 appsettings 片段 / 写入工程 meta | Features 已有 |
| **S2** | 地图：JSON 导入 + 简易 2D 画布（节点/边）；保存 `.sevenproj.json` | — |
| **S3** | Deploy API：物化 Location + 包最小地图；对接现有 Trigger 仿真跑通入库 E2E | **已完成** Validate/Deploy/Undeploy + `Sim_Deployment` |
| **S4** | Player：单机触发面板、流程单据快捷入口、Reset | S3 |
| **S5** | Promote：真机地址表单 + 切换 DeviceComm/通讯包配置 | Phase H 通讯包成熟后完整体验 |
| **S6** | Excel 导入、四向完整地图、SignalR 实时车态 | FourWay / HotStore |

---

## 7. 使用方法（实施人员）

### 7.1 日常联调

```powershell
# 终端1：API（开发可开 Wms+Bus+Stacker+Simulator）
cd Seven.Master/Seven.Net8
dotnet run --project Seven.WebApi

# 终端2：模拟器
cd Seven.Master/Seven.Simulator
npm install
npm run dev
```

1. 打开模拟器 → **Features**：勾选本项目包组合 → 保存到工程。  
2. **地图**：导入或绘制 → Deploy。  
3. **仿真**：Start → 单机触发 / 跑入库出库 → 观察运输单与库存。  
4. **生产**：Promote → 填 PLC/设备地址 → 用 `Seven.Vue3` 做业务验收 → 上线。

### 7.2 与 Seven.Vue3 分工

| 工具 | 用途 |
|------|------|
| Seven.Simulator | 工程、地图、仿真节拍、Promote |
| Seven.Vue3 | 日常单据、库存、联锁、接口日志、SCADA 运维 |

### 7.3 模式对照（对标 RCS 三档，收敛为两态）

| 态 | 含义 | Seven 对应 |
|----|------|------------|
| Simulation | 仿真身份 + Trigger/回环 | `runtimeMode=Simulation`，`SIM_` 设备 |
| Production | 真机 | Promote 后，`DeviceComm`/通讯包 |

（RCS 的 Gateway vs ModbusRgv 仿真档，在 Seven 中收敛为：语义 Trigger 轻仿真 → 通讯包协议保真仿真 → Promote；不在首期做双档并存复杂度。）

---

## 8. 非目标（首期）

- 在模拟器内重做完整 WMS 单据 CRUD（跳转或嵌入 Vue3 即可）  
- 照搬 RCS Three.js 全量 3D  
- 引用 RCS4Shuttle / LES 程序集  
- 无 Deploy 直接改生产库地图  

---

## 9. 决策摘要

| 决策 | 选择 |
|------|------|
| 形态 | 独立 `Seven.Simulator` SPA，栈对齐 `Seven.Vue3` |
| 后端 | 同进程 WebApi + `Features.Simulator` |
| 闭环 | Features → 地图 Deploy → 仿真 → Promote 生产 |
| 通讯 | 仿真走 TriggerPort；生产走通讯包/DeviceComm |
| 参考 | RCS simulation-spa 流程与工程模型；不搬巨石 |
