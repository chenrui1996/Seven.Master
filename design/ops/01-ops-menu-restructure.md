# 运维菜单重构：取消「执行运维」，下沉到各 WCS 包

状态：已落地（菜单种子 / Features / Vue 运维页 / Ops API）  
日期：2026-09-12  
对照实现：[`DbSeeder.cs`](../../Seven.Net8/Seven.Infrastructure/Persistence/DbSeeder.cs)、[`features.ts`](../../Seven.Vue3/src/stores/features.ts)、[`MainLayout.vue`](../../Seven.Vue3/src/layout/MainLayout.vue)、`/api/Wcs/{FourWay|Stacker}/Ops`  
参考：RCS4Shuttle [`docs/ShttleInfAlign/4.运维设计.md`](D:/Junheinrich/Shuttle/RCS4Shuttle/docs/ShttleInfAlign/4.运维设计.md)、[`WcsWebPageConfigDeployService.cs`](D:/Junheinrich/Shuttle/RCS4Shuttle/LES.BLL/Service/ExtendService/WCS/WcsWebPageConfigDeployService.cs)

---

## 1. 决策摘要

| 决策 | 内容 |
|------|------|
| **取消** | 顶级目录「执行运维」/`WcsOpsFolder`（`OrderNo=99` 钉底） |
| **新增** | `四向车WCS → 运维 (FwOpsFolder)`、`立库WCS → 运维 (StkOpsFolder)` |
| **迁入四向/立库监控** | `Scada/Floor2d`（按 `PackId` 过滤地图） |
| **下沉联锁** | `CtlMode` 进入各包运维「联锁与模式」（写权威见 §5） |
| **迁入系统管理** | `IfcApiLog`（接口日志），不再作为设备运维入口 |
| **废止** | 前端「OrderNo≥90 或 执行运维 → 钉轨底」对运维目录的特殊处理 |

硬规则（全仓运维统一）：

1. **账本只在 `Wms_*`**；运维入库若走正式账本须经 WMS API；包内轻量入库不得另造可扣减库存表。
2. **路径/交通只在包内**（`Stk_*` / `Fw_*` + HotStore）；不进编排总线。
3. **运维动作默认走调度主干**（`AcceptLeg` / 包内 Registered 等价入口）；手动控制为折叠旁路。
4. **双空闲门控**：WCS 任务态 Free 才可派新调度；设备段空闲才可下发下一段。

---

## 2. 现状 → 目标对照

### 2.1 现状（种子）

```text
仓储WMS (WmsFolder, 6)
立库WCS (WcsFolder, 7)
  └── 仿真与监控 / 策略 / 主数据 / 任务
四向车WCS (FourWayFolder, 8)
  └── 仿真 / 策略 / 主数据 / 任务
执行运维 (WcsOpsFolder, 99)          ← 删除
  ├── 运行模式/联锁  CtlMode
  ├── 接口日志       IfcApiLog
  └── 2D看板         ScadaFolder → /Scada/Floor2d
```

### 2.2 目标菜单树

```text
仓储WMS (WmsFolder)                    ← 不变（单据作业含入库/出库快捷）

立库WCS (WcsFolder)
├── 运维 (StkOpsFolder)                ← 新增，OrderNo=0（置顶）
│   ├── 监控面板     StkOpsMonitor     /Wcs/Stacker/Ops/Monitor
│   ├── 堆垛机运维   StkOpsSrm         /Wcs/Stacker/Ops/Srm
│   ├── 申请点运维   StkOpsRequest     /Wcs/Stacker/Ops/RequestPoint
│   └── 联锁与模式   StkOpsCtlMode     /Wcs/Stacker/Ops/ControlMode
├── 仿真 (WcsSimFolder)                ← 仅保留「堆垛仿真触发」；运输单监控可挂运维监控侧栏入口
├── 策略 / 主数据 / 任务               ← 保留

四向车WCS (FourWayFolder)
├── 运维 (FwOpsFolder)                 ← 新增，对齐 RCS Menu_OpsPanel，OrderNo=0
│   ├── 监控面板     FwOpsMonitor      /Wcs/FourWay/Ops/Monitor
│   ├── 入库         FwOpsInbound      /Wcs/FourWay/Ops/Inbound
│   ├── 穿梭车运维   FwOpsShuttle      /Wcs/FourWay/Ops/Shuttle
│   ├── 提升机运维   FwOpsHoist        /Wcs/FourWay/Ops/Hoist
│   └── 联锁与模式   FwOpsCtlMode      /Wcs/FourWay/Ops/ControlMode
├── 仿真 / 策略 / 主数据 / 任务        ← 保留（仿真触发可链到运维监控）

系统管理
└── 接口日志         IfcApiLog         /Platform/InterfaceLog  （Parent 改挂系统管理）
```

运维子页顺序（四向，对齐 RCS）：**1 监控 → 2 入库 → 3 穿梭车运维 → 4 提升机运维 → 5 联锁与模式**。  
立库顺序：**1 监控 → 2 堆垛机运维 → 3 申请点运维 → 4 联锁与模式**。

---

## 3. 菜单迁移动作表（实现阶段执行）

| TableName | 现 Parent | 目标 Parent | Url（建议） | 说明 |
|-----------|-----------|-------------|-------------|------|
| `WcsOpsFolder` | 根 | **删除** | — | 目录及钉底逻辑废止 |
| `CtlMode` | WcsOpsFolder | **拆为两份** 或 单页带 Pack 切换 | `/Wcs/{Pack}/Ops/ControlMode` | 见 §5；旧 `/Platform/ControlMode` 可 301/别名一期 |
| `IfcApiLog` | WcsOpsFolder | 系统管理 | `/Platform/InterfaceLog` | Url 可不变，仅改 ParentId |
| `ScadaFolder` | WcsOpsFolder | **删除叶子** | — | 能力并入 `StkOpsMonitor` / `FwOpsMonitor` |
| `StackerTrigger` | WcsSimFolder | 可保留 | `/Wcs/Stacker/Trigger` | 运维监控提供「打开仿真触发」快捷链 |
| `FourWayTrigger` | FwSimFolder | 可保留 | `/Wcs/FourWay/Trigger` | 同上 |
| `BusTransportOrder` | WcsSimFolder | 建议改挂 StkOpsMonitor 侧入口或总线只读页仍挂立库 | 现 Url | 跨包时两边监控均可链到总线单 |

新增叶子（实现时写入种子）：

| TableName | MenuName | Parent | Auth 建议 |
|-----------|----------|--------|-----------|
| `FwOpsFolder` | 运维 | FourWayFolder | — |
| `FwOpsMonitor` | 监控面板 | FwOpsFolder | Search,Update,Export |
| `FwOpsInbound` | 入库 | FwOpsFolder | Search,Add,Update |
| `FwOpsShuttle` | 穿梭车运维 | FwOpsFolder | Search,Update |
| `FwOpsHoist` | 提升机运维 | FwOpsFolder | Search,Update |
| `FwOpsCtlMode` | 联锁与模式 | FwOpsFolder | Search,Update |
| `StkOpsFolder` | 运维 | WcsFolder | — |
| `StkOpsMonitor` | 监控面板 | StkOpsFolder | Search,Update,Export |
| `StkOpsSrm` | 堆垛机运维 | StkOpsFolder | Search,Update |
| `StkOpsRequest` | 申请点运维 | StkOpsFolder | Search,Update |
| `StkOpsCtlMode` | 联锁与模式 | StkOpsFolder | Search,Update |

---

## 4. Features / 前端过滤规格

现有 [`features.ts`](../../Seven.Vue3/src/stores/features.ts)：

```text
WcsOpsFolder → orchestrationBus || wms
```

目标：

| key / path | 可见条件 |
|------------|----------|
| `FwOpsFolder` 及 `FwOps*`、`/Wcs/FourWay/Ops` | `wcsPacks.fourWay` |
| `StkOpsFolder` 及 `StkOps*`、`/Wcs/Stacker/Ops` | `wcsPacks.stacker` |
| `IfcApiLog` | `orchestrationBus || wms`（系统管理下，不依赖运维目录） |
| `WcsOpsFolder` | **恒 false**（迁移完成后可删分支） |

[`MainLayout.vue`](../../Seven.Vue3/src/layout/MainLayout.vue) 钉底判断：

```text
// 现状
table === 'wcsopsfolder' || name === '执行运维' || OrderNo ≥ 阈值

// 目标
移除对 WcsOpsFolder /「执行运维」的钉底；运维目录随包菜单正常排序（OrderNo=0 置顶于包内即可）
```

---

## 5. 联锁与模式写权威

| 方案 | 说明 | 选用 |
|------|------|------|
| **A. 单表共享 + 包作用域过滤** | 仍用 `Ctl_*`；UI 按当前包显示相关联锁项；急停可为仓级全局 | **采用** |
| B. 每包复制控制表 | 易双写冲突 | 不用 |

规则：

- **仓级急停 / 全局联锁**：任一包运维页可写，写后两边只读刷新；审计打 `PackId` 来源。
- **包专属模式**（如仅四向禁止跨层）：只在对应包运维页可写。
- API：`PUT /api/Platform/ControlMode` 保留；包运维页带 `scope=fourway|stacker|warehouse`。

---

## 6. 2D 监控合并规则

| 能力 | 立库监控 | 四向监控 |
|------|----------|----------|
| 货位/巷道着色 | `PackId=stacker` | `PackId=fourway` + 按层切换 |
| 任务抽屉 | PutAway / Retrieval / DeviceTask | ShuttleTask / Hoist* / PutAway / Retrieval |
| 选设备快捷 | 选堆垛机 → 跳转堆垛运维任务树 | 选车 → 空闲则指定点/充电；执行中则任务详情 |
| 入库表单 | **不承载**（正式入库走 WMS） | **不承载**（独立「入库」页） |
| 出入口标记 | 申请点可视化 | Gateway / Hoist 口 tip 可标记 |

旧 `/Scada/Floor2d`：实现阶段改为重定向到按 Features 选择的默认监控，或只读兼容页。

---

## 7. 与 RCS4Shuttle 菜单对齐

| RCS `Menu_*` | Seven 目标 |
|--------------|------------|
| `Menu_OpsPanel` | `FwOpsFolder` |
| `Menu_ShuttleFloorPlan` | `FwOpsMonitor` |
| `Menu_OpsInbound` | `FwOpsInbound` |
| `Menu_RgvManual` | `FwOpsShuttle` |
| `Menu_HoistManual` | `FwOpsHoist` |
| （无对等） | `FwOpsCtlMode` / `StkOps*` |

立库无 RCS 原样页面；结构对称，内容见 [`../srm-wcs/03-end-to-end-and-ops.md`](../srm-wcs/03-end-to-end-and-ops.md)。

---

## 8. 实现阶段验收清单（文档先行，代码后做）

- [ ] 种子不再创建 `WcsOpsFolder`；已有库迁移脚本改 Parent / 删空目录
- [ ] Features 过滤与钉底逻辑按 §4 更新
- [ ] 四向运维五页、立库运维四页路由可打开（可先占位壳）
- [ ] 接口日志出现在系统管理下且权限正确
- [ ] 无「执行运维」菜单文案与 i18n 键残留（或映射到新名）

---

## 9. 关联文档

| 文档 | 关系 |
|------|------|
| [../shuttle-wcs/05-ops-menu-and-process.md](../shuttle-wcs/05-ops-menu-and-process.md) | 四向运维页详细规格 |
| [../srm-wcs/03-end-to-end-and-ops.md](../srm-wcs/03-end-to-end-and-ops.md) | 立库运维页详细规格 |
| [../../testplan/cross-pack-closed-loop.md](../../testplan/cross-pack-closed-loop.md) | 菜单可见性回归用例 |
