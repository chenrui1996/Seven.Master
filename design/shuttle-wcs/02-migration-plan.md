# Seven 四向车（FourWay / Shuttle）WCS 迁移方案

状态：F0–F5 已落地；**WCS-FFU（FU1–FU4）已落地**；F6 待实施  
日期：2026-08-29  
前缀：`Fw.*`；PackId=`fourway`  
依据：[01-rcs4shuttle-flows](./01-rcs4shuttle-flows.md)、[`../wms/05-masterdata-and-allocation-storage.md`](../wms/05-masterdata-and-allocation-storage.md)；产品文档 [`../../doc/20-WMS与WCS实现说明.md`](../../doc/20-WMS与WCS实现说明.md) 第三部分  

---

## 1. 职责切分

```text
WMS
  · Zone / Layer / Aisle / Location 骨架（Fw. 前缀 + PackId）
  · 库存账本；Storage/Retrieval 业务意图 → Bus

Bus
  · TransportOrder / Leg；跨包交接（如 Stk↔Fw、Fw↔输送）
  · 不存层内路网流量、不做选层

FourWay Pack
  · Fw_ Assignment：层→巷→位
  · Fw_Map / Node / Route / Traffic（HotStore）
  · Fw_ShuttleTask / ExecPath；Hoist 子模块（可同包分期）
  · 停车账本、派车、申请点（Shuttle/Hoist EP/AP）
```

---

## 2. 表映射（RCS → Seven）

| RCS | Seven |
|-----|-------|
| Zone (DeviceType=Shuttle) | `Wms_Zone` PackId=fourway，`Fw.Z-*` |
| **Layer** | **`Wms_Layer`（补）** 或过渡期 Zone 子级；推荐独立表 `Wms_Layer`，`Fw.L-*` |
| Aisle | `Wms_Aisle` + `Fw_AisleProfile`（MaxShuttleCount 等） |
| Location | `Wms_Location` `Fw.B-*`/`Fw.N-*`；几何进 `Fw_Node` |
| AssignmentPolicy (Layer) | **`Fw_LayerPolicy` / `Fw_AssignmentPolicy`**（勿与 Stk_ 共用一张运行策略表） |
| StorageTask4Shuttle | Bus Leg + `Fw_PutAwayTask`（或统一包任务名）含 LayerCode/AisleCode |
| ShuttleTask / Exec* | `Fw_ShuttleTask` / `Fw_ShuttleTaskPath`（已有雏形则对齐字段） |
| Hoist* | `Fw_Hoist*`（P1） |
| LesRoute* | `Fw_Route` / `Fw_RouteFlow`（按层 MAP） |

库存只认 `Wms_Stock.LocationCode` → `Fw.*` 货位。

---

## 3. 分配契约

```text
IWcsLocationAllocator (packId=fourway)
  AllocateInbound(ctx)
    → Stage Layer → Stage Aisle → Stage Location
    → AllocationResult { LayerCode, AisleCode, LocationCode }

IWcsLocationSchema (fourway)
  Hierarchy = [ Zone, Layer, Aisle, Location ]
  NormalizeCode → Fw.
```

金样例：从 RCS `SelectLayerAndAisle` / `SelectLocation` 抽测例迁单测。

---

## 4. 阶段计划

| 阶段 | 内容 | 验收 | 进度 |
|------|------|------|------|
| **F0** | 文档 + `Wms_Layer` 设计拍板（与统一主数据裁定一致） | 评审 | ✅ |
| **F1** | `Wms_Layer` + Location/Aisle 挂 Layer；前缀校验 | 同仓 Stk+Fw 共存 | ✅ |
| **F2** | `Fw_*Policy` + Allocator 三阶段 | 与 RCS 金样一致 | ✅ |
| **F3** | PutAway 任务 + Bus Leg + 仿真 Trigger/地图 Deploy 写 Fw 码 | 入库 E2E | ✅ |
| **F4** | ShuttleTask 路径/流量 + HotStore | 层内搬运 | ✅ |
| **F5** | Hoist 跨层 + 停车账本 + 出库 Retrieval | 跨层 E2E | ✅ |
| **FFU** | FU1 双包 NG / FU2 Path / FU3 Parking·MaxShuttle / FU4 Hoist 补扫·口·错误闸 | 见 [doc/20 第三部分](../../doc/20-WMS与WCS实现说明.md) | ✅ |
| **F6** | 通讯/仿真 Promote（对齐 RCS SimDeploy 概念，走 Seven.Simulator） | 联调 | |

---

## 5. 明确不迁 / 后置

- 整包复制 `ShuttleExecuteStack` 巨型类 → 按用例切开服务  
- 与 SRM 共用一张 AssignmentPolicy 运行表（Seven 分 `Stk_`/`Fw_`）  
- 程序集引用 RCS4Shuttle  

---

## 6. 与现有 Seven FourWay

在现骨架上**增量**：Layer 主数据、三阶段分配、Hoist、停车；地图 Deploy 输出 `Fw.` 前缀码。
