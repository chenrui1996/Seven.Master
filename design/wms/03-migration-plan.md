# WMS 迁入 Seven 完整方案

状态：M6 已落地（LES 主数据+库存迁移脚本与前缀对账）；后续按项目现场执行  
日期：2026-08-29  
依据：[01-les2-wms-flows](./01-les2-wms-flows.md)、[02-location-multi-pack-prefix](./02-location-multi-pack-prefix.md)、[06-les-data-migration](./06-les-data-migration.md)、现有 Seven 骨架
---

## 1. 目标

把 LES2 WMS **业务语义**迁入 `Seven.Net8` + `Seven.Vue3` + `Seven.App`，并与可插拔 WCS 包、薄总线对齐；**不**引用 LES2 程序集、**不** 1:1 复制多套单据表。

---

## 2. 目标架构（WMS 视角）

```text
ERP/MES ──► Seven.WebApi
              ├─ WMS：主数据(前缀码) · 库存 · 三单 · 拣选/预约
              ├─ Bus：TransportOrder / Leg（入≈StorageTask 意图，出≈RetrievalTask 意图）
              ├─ WCS Packs：结构+分配+设备执行（按 PackId）
              ├─ Seven.Vue3：运维/单据
              └─ Seven.App：PDA（原 LesApp）
```

交接语义映射：

| LES2 | Seven |
|------|-------|
| StorageTask | 入库 Detail 完成后 → `Bus_TransportOrder`（Ref=Inbound）→ Leg@Pack → 包内 PutAway |
| RetrievalTask | 出库预约/拣选后 → `Bus_TransportOrder`（Ref=Outbound/Pick）→ Leg@Pack → 包内 Retrieval |
| TransportTask+DeviceExec | **包内**任务（Stk_/Fw_），总线不存输送边流量 |

---

## 3. 差距与工作包

### WP-A 主数据与前缀（决策 B）

| 项 | 动作 |
|----|------|
| Warehouse.EnabledPackIds | 新字段/子表；校验 ≥1 Pack |
| Zone/Location + PackId | 强制前缀；补 `Wms_Aisle` |
| 多层主数据 + 分配策略落表 | 见 [05](./05-masterdata-and-allocation-storage.md)：Wms 骨架共用，策略分 `Stk_*`/`Fw_*` |
| LocationSchema | 每包实现校验与 UI 提示 |
| HandoverLink | 跨包码必须分属不同 PackId |
| 数据迁移工具 | LES 裸码 → 前缀码脚本 |

### WP-B 库存与容器

| LES2 | Seven 动作 |
|------|------------|
| Stock 无 Location | `Wms_Stock.LocationCode` 直存；收货/移库同步 |
| 组盘即入账 | **保持**（兼容 LES 习惯）；取消组盘走补偿 |
| BookQuantity | 出库预约字段补齐；与拣选任务联动 |

### WP-C 入库

| 阶段 | 内容 |
|------|------|
| 已有 | Inbound 单、Receive、可选建运输单 |
| 补齐 | Detail 级组盘、推荐库区（调包 Allocator）、Storage 意图状态机（Create/Aisle/Location/Finish 映射到 Order+Leg+PutAway） |
| PDA | 见 [04](./04-seven-app-pda.md) 收货/上架 |

### WP-D 出库

| 阶段 | 内容 |
|------|------|
| 已有 | Outbound Approve → 运输单；完成落账 |
| 补齐 | PreAssign / Assign（预约）、PickingTask（或等价行）、Retrieval 意图、WcsGroupNo/WcsPri 滚动（包或总线轻量协调） |
| 包侧 | Stacker 补 Retrieval 对称任务（见 srm-wcs） |

### WP-E 盘点

对齐 CycleCount：预约 →（可选）拉出工位运输 → 录入 → 差异调账；PDA 录入 API。

### WP-F 接口

| LES2 | Seven |
|------|-------|
| ERP 101/102/… | `Ifc_` 日志 + 显式 Import API；Outbox 回传（可选） |
| AppController | 新建 `api/pda/*`，契约面向 Seven DTO |

### WP-G 明确不迁

- Waybill 产品化（二期）
- 面粉/吨包等项目特化策略（插件化后再迁）
- LCI Socket / 旧 ExecuteStack 缓存字典形态
- 多套同构单据表名

---

## 4. 阶段计划

| 阶段 | 交付 | 验收 | 进度 |
|------|------|------|------|
| **M0** | 本文档 + srm-wcs + 前缀规范落地到实体设计 | 评审通过 | ✅ |
| **M1** | PackId/前缀/Aisle/Layer/EnabledPackIds + Schema/Allocator | 单测：错误前缀拒绝；一仓两包可共存 | ✅ |
| **M2** | 入库 Detail 组盘 + 调 Stacker Allocator + E2E 仿真 | 组盘→SUDR→上架完成→账本位置正确 | ✅ |
| **M3** | 出库预约/拣选 + Retrieval 包任务 + 组优先级 | 出库 E2E；同组滚动 | ✅ |
| **M4** | Seven.App 脚手架 + 平库收货/上架竖切 | PDA 真机扫码走通 | ✅ |
| **M5** | 盘点 PDA + 运维页补齐 | 与 Vue3 一致 | ✅ |
| **M6** | LES 数据迁移脚本（主数据+库存） | 前缀转换、抽样对账 | ✅ |

---

## 5. 与 Features / UI

- `Features.Wms` / `WcsPacks.*`：**仅控制菜单**。
- 产品规则「WMS 必须选 WCS」用 **Warehouse.EnabledPackIds** 与 API 校验，不靠关掉后端 DI。

---

## 6. 测试策略

- 单测：前缀、Allocator 契约、入库出库状态机。
- 集成：InMemory TriggerPort E2E（已有入库堆垛可扩展出库）。
- PDA：契约测试 `api/pda` + 手工扫码清单。

---

## 7. 风险

| 风险 | 缓解 |
|------|------|
| 一仓多包坐标字段混用 | Schema 分包校验；UI 按 Pack 切换表单 |
| 前缀改造破坏旧仿真工程 | Simulator Deploy 写前缀码；文档/示例工程升级 |
| 出库组批复杂度 | M3 先做单组顺序；滚动调度对齐 LES 后再优化 |
| PDA 双轨立库/平库 | M4 先平库；立库收货只建 Storage 意图、上架靠自动化 |
