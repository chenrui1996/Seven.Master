# 立库（堆垛机）WCS：端到端流程与运维设计

状态：设计定稿（本阶段仅文档）  
日期：2026-09-12  
运行时调研：[`01-runtime-from-les.md`](./01-runtime-from-les.md)  
实现对照：[`doc/20`](../../doc/20-WMS与WCS实现说明.md) 第二部分  
运维 IA：[`../ops/01-ops-menu-restructure.md`](../ops/01-ops-menu-restructure.md)

---

## 0. 分层与硬规则

```text
WMS（组盘/出库审核）→ Bus Leg(PackId=stacker) → Stk_* 包
  → DestinationRequest / PathDispatcher / DeviceTask
  → IEquipmentTriggerPort（仿真或通讯）
  → SegmentFeedback → Bus Complete → WMS 落位/扣账
```

| # | 硬规则 |
|---|--------|
| 1 | 账本只在 `Wms_*`；`Stk_*` 仅任务与路网 |
| 2 | 路径流量在包内 `Stk_Route*`，不进 Bus |
| 3 | 运维默认走调度主干；手动/仿真触发为旁路 |
| 4 | 双空闲：WCS 任务可派 + 当前 DeviceTask 段空闲才可下发下一段 |
| 5 | 货位前缀 `Stk.`；与四向交接走 `Wms_HandoverLink` |

---

## 1. 主数据与任务对象

```text
Wms_Warehouse → Zone(PackId=stacker) → Aisle → Location(Stk.*)
包内：Stk_RequestPoint / AssignmentPolicy / LocationProfile(双深)
      Stk_Route / RouteFlow / DeviceCoder
```

| 对象 | 用途 |
|------|------|
| `Stk_PutAwayTask` | 入库上架（与 Leg 1:1） |
| `Stk_RetrievalTask` | 出库；`WcsGroupNo` / `WcsPri` |
| `Stk_DeviceTask` | 设备段；`Seq` / `ExeStackCode` |
| Bus TO/Leg | 跨系统编排锚点 |

---

## 2. 入库全流程（正式）

```text
BuildPallet（StackerInboundAllocator：巷→位，常 Booking）
  → AcceptLeg → PutAway(Accepted)
  → DestinationRequest @ AisleRequest
       · CheckResult≠OK → Reject + PutAway Failed
       · SelectAisle → AisleAssigned → Dispatch(Ep) + DeviceTask
  → （可选）LocationRequest → SelectLocation+Book → Dispatch(Bin)
  → （可选）BlockingPoint：作废在途、释放预约、重选
  → PathDispatcher：寻路拆腿；仅首段 Dispatched
  → SegmentFeedback → 释流 → 下一段 → …
  → PutAway Completed → Bus → WMS 容器落目标位
```

申请点类型：AisleRequest(10) / LocationRequest(20) / BlockingPoint(30)。

双包：源点为启用的**四向**申请点且外形 NG 时，堆垛侧静默。

---

## 3. 出库与深浅移库

```text
Outbound Approve → AcceptRetrieval(Accepted)
  → TryDispatch：同组更小 Pri 未终态 → Suspended
  → DepthGuard：深位被浅挡
       · 深位 Suspended，Pri+1
       · 建 Bus StackerTransfer（浅→同巷空位）
  → PathDispatcher 下发
  → 段完成 → 滚动下一 Pri → … → Stock Ship
```

---

## 4. 与仿真 Trigger

| API | 含义 |
|-----|------|
| `destination-request` | SUDR：外形/源址 → 分配 → 首段 |
| `segment-feedback` | 段完成推进 |

运维监控应提供跳转「堆垛仿真触发」页的入口（种子可保留 `StackerTrigger`）。

---

## 5. 运维菜单（StkOpsFolder）

对齐 ops/01；立库无提升机、无独立「运维入库」页（正式入库走仓储 WMS；需要时用 WMS 入库快捷）。

| Order | TableName | 名称 | Url |
|------:|-----------|------|-----|
| 1 | StkOpsMonitor | 监控面板 | `/Wcs/Stacker/Ops/Monitor` |
| 2 | StkOpsSrm | 堆垛机运维 | `/Wcs/Stacker/Ops/Srm` |
| 3 | StkOpsRequest | 申请点运维 | `/Wcs/Stacker/Ops/RequestPoint` |
| 4 | StkOpsCtlMode | 联锁与模式 | `/Wcs/Stacker/Ops/ControlMode` |

可见条件：`wcsPacks.stacker`。  
原「仿真与监控」下运输单监控：监控页侧栏链到 `/Wcs/Bus/TransportOrder`。

---

## 6. 监控面板（StkOpsMonitor）

1. **2D/巷道视图**（`PackId=stacker`；吸收原 Scada）  
2. **任务抽屉**：PutAway / Retrieval / DeviceTask（执行中、排队、失败）  
3. **选中堆垛机/申请点**：基本态 +「打开运维任务树」  
4. **不承载** WMS 入库表单；可展示最近 Bus Leg 摘要  

快捷：跳转仿真触发、打开联锁页。

---

## 7. 堆垛机运维（StkOpsSrm）

四段式（对称四向穿梭页，设备类型换为 SRM）：

| # | 区块 | 默认 |
|---|------|------|
| 1 | 状态芯片：模式 / 巷道 / 当前段 / 联锁 | 常显 |
| 2 | 常用：重发当前段、强制完成任务/段、暂停接单 | 展开 |
| 3 | 任务树：PutAway\|Retrieval → DeviceTask[]（含 RouteFlow 精简） | 展开 |
| 4 | 手动/仿真控制（点到点、写 Trigger） | **默认折叠** |

门控：非 Free 禁止新派；段未完成禁止重发下一段。

---

## 8. 申请点运维（StkOpsRequest）

- 列表：`Stk_RequestPoint` 类型、启用态、绑定 DeviceCoder。  
- 动作：临时停用申请点（拒 SUDR）、查看最近 DestinationRequest 流水。  
- Blocking 场景：展示「已触发重分配」任务链接到堆垛运维树。

---

## 9. 联锁与模式（StkOpsCtlMode）

- 仓级急停 + stacker 作用域模式（与四向共享 `Ctl_*`，见 ops/01 §5）。  
- 盘点锁仓只读提示：`Wms_Warehouse.IsCycleCountLocked`（分配已跳过，运维页明示原因）。

---

## 10. API 契约草案

前缀：`/api/Wcs/Stacker/Ops`

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/board` | 活动任务板 |
| GET | `/task-tree?deviceNo=` | 任务树 |
| POST | `/force-complete` | 强制完成任务/段 |
| POST | `/resend` | 重发段 |
| POST | `/request-point/{id}/disable` | 临时停用 |
| GET/PUT | `/control-mode` | scope=stacker |

仿真 Trigger API 保持不变。

---

## 11. 异常恢复矩阵

| 场景 | 入口 | 动作 |
|------|------|------|
| SUDR 拒收 | 监控 / 申请点 | 查 CheckResult；外形/地址 |
| 无巷道可分 | 策略页 + 运维 | 调权重/清锁/解锁盘点 |
| 双深孤深 | 任务树 | 先完成/取消 Transfer |
| 段停滞 | 堆垛运维 | 强制完成或重发 |
| 路网满容 | 监控占边 | 等释流或取消在途 |
| 急停 | 联锁 | 恢复后重试 Dispatch |

---

## 12. 验收口径

1. 仅 `stacker`：组盘 → SUDR → 多段反馈 → 库存到 `Stk.*`。  
2. 出库 Pri 滚动 + 深浅 Transfer 后再出深位。  
3. 运维四页在 Features 开关下可见；无「执行运维」。  
4. 强制完成/重发受双空闲门控；单测覆盖拒批路径。
