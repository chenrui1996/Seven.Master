# 四向车 WCS 运维菜单与流程规格

状态：设计定稿（本阶段仅文档）  
日期：2026-09-12  
菜单 IA：[`../ops/01-ops-menu-restructure.md`](../ops/01-ops-menu-restructure.md)  
全流程：[`04-end-to-end-flow.md`](./04-end-to-end-flow.md)  
参考：RCS4Shuttle `4.运维设计.md` / `5.业务设计.md` / `OpsPanelApiController` → `/api/ops/*`

---

## 1. 目标与边界

**目标**：在「四向车WCS → 运维」下提供可闭环的现场运维能力（监控、轻量入库、单车、提升机、联锁），交互对齐 RCS 四段式，API 落在 Seven 风格路径下。

**不做（本设计阶段）**：实现页面与 Controller；正式 ERP 收货全链路（走仓储 WMS）。

**做**：页面信息架构、门控、任务树、异常操作矩阵、API 契约草案。

---

## 2. 菜单与路由

| Order | TableName | 名称 | Url |
|------:|-----------|------|-----|
| 1 | FwOpsMonitor | 监控面板 | `/Wcs/FourWay/Ops/Monitor` |
| 2 | FwOpsInbound | 入库 | `/Wcs/FourWay/Ops/Inbound` |
| 3 | FwOpsShuttle | 穿梭车运维 | `/Wcs/FourWay/Ops/Shuttle` |
| 4 | FwOpsHoist | 提升机运维 | `/Wcs/FourWay/Ops/Hoist` |
| 5 | FwOpsCtlMode | 联锁与模式 | `/Wcs/FourWay/Ops/ControlMode` |

父目录：`FwOpsFolder`（见 ops/01）。可见条件：`wcsPacks.fourWay`。

---

## 3. 共享原则（四页通用）

### 3.1 双空闲

| 层级 | 条件 | 允许的操作 |
|------|------|------------|
| WCS Free | 该车无未完成调度意图（或显式可抢占策略关闭） | 指定点、充电、接新 PutAway/Retrieval |
| 设备段空闲 | 当前 Exec/段已完成且无在途指令 | 下发下一段 Step / 重发当前段 |

按钮：`btn-ops-idle` 仅在 WCS Free 可点；段级操作看段态。

### 3.2 任务树（共享组件）

对标 RCS `LesOpsTaskPanel`：

```text
PutAway | Retrieval | Direct | Charge
  └── ShuttleTask
        └── Path[]（可折叠）
        └── （跨层）HoistTask
              └── HoistExecTask[]
```

要求：轮询刷新时**按签名保留折叠态**；指令详情用**当前页大模态**，不新开标签。

### 3.3 危险操作

强制完成、取消、急停恢复、手动写设备：二次确认；Gateway/只读仿真模式拒绝写设备旁路。

---

## 4. 监控面板（FwOpsMonitor）

### 4.1 布局

1. **层切换 + 2D 地图**（吸收原 Scada；仅 `PackId=fourway`）  
2. **任务抽屉**（默认收起）：执行中 / 已分配未执行 / 待分配  
3. **选中小车侧栏**：基本信息；执行中 →「任务详情」；空闲 → 指定点 / 充电 / 结束充电  

### 4.2 行为规则

- **不承载入库表单**（入库见独立页）。  
- 库位 tip：可标记/取消 Gateway 出入库口（权限 Update）。  
- 指定点：对话框不遮挡地图；目的填 XYZ **或** 库位编码（分段控件切换）。  
- 顶部不再单独放「全局指定点」主按钮（入口在选车或穿梭运维）。

### 4.3 数据源

- 地图快照：节点占用、车位置、任务着色  
- 任务列表：`Fw_PutAwayTask` / `Fw_RetrievalTask` / `Fw_ShuttleTask` 活动集  
- 动作：调用 §7 Ops API

---

## 5. 入库页（FwOpsInbound）

### 5.1 表单

| 字段 | 说明 |
|------|------|
| 托盘号 | Container.Code |
| 物料名 / 数量 | 轻量模式写入 Container.ClassifyFlag / Quantity |
| 出入口 | Gateway 列表或地图点选 |
| 策略 | 自动 / 指定层 / 指定货位（单选） |
| 指定小车 | 可选 |
| 同步 WMS | 可选开关；开则走 `BuildPallet` 正式账本 |

指定货位：「选择货位」打开 2D；不可选：已占用/已分配/浅深阻挡/正在入库巷道；返回 `Pickable` / `PickBlockReason`。

### 5.2 编排

```text
提交
  → 全库出入互斥门控
  → 若同步 WMS：InboundOrderService.BuildPallet…
  → 否则：CreateAndDispatch（轻量）
       · Container upsert
       · Fw_PutAway + Shuttle 注册调度（对标 RegisteredShuttleTask）
  → 失败补偿：回滚 Container/预约/任务半成品
```

互斥：存在未完成入库 → 拒出库类创建；存在未完成出库 → 拒本页入库。

---

## 6. 穿梭车运维（FwOpsShuttle）

四段式（对标 RCS RgvManual）：

| # | 区块 | 默认 |
|---|------|------|
| 1 | 状态芯片：连接 / 工况 / 层巷址 / 电量 / WCS Free | 常显；详细 Modbus 折叠 |
| 2 | 常用功能：指定点、充电、结束充电、常用设备设置 | 展开 |
| 3 | 当前任务树 + 强制完成 / 重发 | 展开 |
| 4 | 手动控制：指令 / 任务 / 点到点 | **默认折叠** |

强制完成语义：现场动作已完成、故障复位后推进该段；不得在段未空闲时跳步。

---

## 7. 提升机运维（FwOpsHoist）

| # | 区块 | 默认 |
|---|------|------|
| 1 | 提升机状态芯片 | 常显 |
| 2 | 常用：指定层下发、设备设置 | 展开 |
| 3 | HoistTask → Exec[] 树 + 强制完成 / 重发 / 指令详情 | 展开 |
| 4 | 手动：Step / 运维信号 | **默认折叠** |

同口排队可视化：Queued / Suspended / Dispatched。

---

## 8. 联锁与模式（FwOpsCtlMode）

- 展示仓级急停 + 四向包作用域模式。  
- 写权威见 [`ops/01` §5](../ops/01-ops-menu-restructure.md)。  
- 急停生效后：监控与运维页只读提示，派发 API 返回业务拒单码。

---

## 9. API 契约草案

前缀：`/api/Wcs/FourWay/Ops`（Cookie/会话与现有 Web 一致；避免无 JWT 401）。  
对标 RCS `/api/ops/*` 能力映射：

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/meta` | 层、Gateway、车列表、策略枚举 |
| GET | `/board` | 活动任务板（监控抽屉） |
| GET | `/task-tree?shuttleNo=` | 单车/单任务树 |
| POST | `/inbound` | 运维入库 CreateAndDispatch |
| GET | `/inbound/pickable-map` | 指定货位可选快照 |
| POST | `/point-dispatch` | 指定点 |
| POST | `/charge` / `/charge/stop` | 充电 |
| POST | `/force-complete` | 强制完成（task/exec/segment） |
| POST | `/resend` | 重发当前段 |
| GET/PUT | `/control-mode` | 联锁（scope=fourway） |

手动控制旁路（若保留）：`/api/Wcs/FourWay/DeviceManual/*`，Gateway 只读拒绝写。

仿真仍用现有：

- `/api/Wcs/Triggers/destination-request`  
- `/api/Wcs/Triggers/segment-feedback`

---

## 10. 异常恢复矩阵（运维操作）

| 场景 | 推荐入口 | 操作 | 门控 |
|------|----------|------|------|
| 车堵在路径 | 监控选车 → 任务树 | 查看占边；取消任务释边 | 权限 Update |
| 段完成但未反馈 | 穿梭运维 | 强制完成该段 | 确认现场 |
| 指令丢失 | 穿梭/提升运维 | 重发 | 段空闲 |
| 入错口 | 入库页 | 取消未执行任务后重开 | 互斥 |
| 提升卡住 | 提升运维 | 强制完成抬升或失败复位 | 同口无冲突 |
| 全局停 | 联锁页 | 急停 / 恢复 | 仓级写 |

---

## 11. UI 样式与组件复用

- 沿用 [`wcs-ops.css`](../../Seven.Vue3/src/styles/wcs-ops.css) 的 `.wcs-ops` / `.ops-panel` / `.ops-toolbar`。  
- 新增：状态芯片、`<details>` 分段、任务树宿主、分段控件（XYZ vs 库位码）。  
- 视觉参考 RCS：`les-ops-panel.css` / `les-device-manual.css`（逻辑对齐，不复制 LES 程序集）。

---

## 12. 实现阶段验收

- [ ] 五页菜单在 `fourWay` 开启时可见；关闭时整目录消失  
- [ ] 监控无入库表单；入库页可完成轻量入库到仿真闭环  
- [ ] 双空闲门控单测 + UI 按钮禁用  
- [ ] 任务树折叠态在 3s 轮询下保持  
- [ ] 无 `WcsOpsFolder` / 旧 Scada 顶级入口
