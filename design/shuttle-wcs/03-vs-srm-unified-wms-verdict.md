# 裁定：统一 Wms_Zone / Aisle / Location（+ Layer）能否同时服务 SRM 与 Shuttle

状态：**已裁定**  
日期：2026-08-29  
对照：[srm-wcs](../srm-wcs/)、[shuttle-wcs/01](./01-rcs4shuttle-flows.md)、[wms/05](../wms/05-masterdata-and-allocation-storage.md)  

---

## 1. 结论（先看）

| 问题 | 结论 |
|------|------|
| 一套 `Wms_*` 账本骨架 + `PackId` + 前缀码，能否满足堆垛与四向？ | **能。RCS4Shuttle 生产模型已证明：SRM/Shuttle 共用 BasicData，只是分配引擎与策略字段用法不同。** |
| 仅 Zone/Aisle/Location 三张是否不够？ | **对四向偏紧。** RCS 有一等公民 **`Layer`**（Zone→Layer→Aisle→Location）。Seven 应在骨架中 **增加 `Wms_Layer`（可空：堆垛不用）**，而不是把「层」勉强等同 `Wms_Zone`。 |
| 是否要按包复制三套 Zone/Aisle/Location？ | **不需要，且不建议**（双源对账、交接位、PDA、库存查询都会裂开）。 |
| 分配策略能否也共用一张 Wms 表？ | **运行策略不要共用。** 流水线不同（巷→位 vs 层→巷→位）；参数表用 `Stk_*Policy` / `Fw_*Policy`。RCS 虽共用 `AssignmentPolicy` 实体，但靠 LayerCode/AisleCode 空值与两套 Engine 区分——Seven 分表更清晰。 |

---

## 2. 结构对照

| 层级 | srm-wcs（堆垛） | shuttle-wcs（四向） | 统一落表 |
|------|-----------------|---------------------|----------|
| 库区 | Zone | Zone（DeviceType=Shuttle） | `Wms_Zone` + PackId |
| 层 | 无 Layer 实体；`Location.Layer` 为货架层号 | **Layer 主数据** | **`Wms_Layer`**（Fw 必填；Stk 不建行） |
| 巷道 | Aisle（EpPoint） | Aisle（属某 Layer；小车数） | `Wms_Aisle` + 包 Profile |
| 货位 | Location Bin | Location + 可选 Fw_Node | `Wms_Location`；路网几何在 `Fw_Node` |
| 分配链 | Aisle → Location | **Layer → Aisle → Location** | 包内 Allocator |
| 策略 | Stk_AssignmentPolicy | Fw_*Policy（层/巷） | 分前缀表 |
| 路径流量 | Stk_Route*（包内） | Fw_Route* / 层 MAP | 分前缀表 |
| 设备任务 | Stk_PutAway/Retrieval/Device | Fw_Shuttle/Hoist | 分前缀表 |

```text
                    ┌─ stacker:  Zone → Aisle → Location
Wms_Warehouse ──────┤
                    └─ fourway:  Zone → Layer → Aisle → Location
                         （均带 PackId + Stk.|Fw. 前缀）
```

---

## 3. 为何「共用骨架」成立

1. **RCS 实证**：`AllocationEngine4SRM` 与 `AllocationEngine4Shuttle` 挂在同一 `LocationExecuteStack`，读写同一 `Location`/`Aisle`/`AssignmentPolicy` 缓存；差异在算法与是否使用 `LayerDic`。  
2. **账本需求单一**：库存、盘点、交接、PDA 只关心「容器在哪个 Code」；Code 前缀隔离包即可。  
3. **包差异外置**：小车数、停车、路网边、提升机点、EpPoint 等进 **Profile / Fw_***，不撑爆 Wms_Location。  
4. **Schema 解释**：`IWcsLocationSchema` 声明各包装载哪些层级；UI/校验按 Pack 切换，避免用错列。

---

## 4. 必须修正的先前简化

原 [wms/05](../wms/05-masterdata-and-allocation-storage.md) 写「四向层即 Zone（Fw.L*）」——**作为权宜可以，但与 RCS 不符**。

**改为：**

- `Wms_Zone`：业务库区（两边都有）  
- `Wms_Layer`：物理楼层（四向）；Code=`Fw.L02`  
- 不要把 Zone 与 Layer 合成一行，否则同区多层、层策略轮转、提升机按层对接都会别扭  

堆垛继续：不建 Layer 行；货架「层」仍用 `Wms_Location.Layer` 整型坐标。

---

## 5. 风险与约束

| 风险 | 缓解 |
|------|------|
| Location 列语义混用（Z vs Layer 坐标） | Schema 校验；文档标明 Stk 的 Layer 字段≠ Fw 的 Wms_Layer |
| 策略表若强行合一 | Seven 分 Stk_/Fw_；迁移时拆 RCS AssignmentPolicy |
| 同仓两包码冲突 | 强制前缀 + PackId 冗余列 |
| Aisle 在四向「很像轨道」 | 仍用 Wms_Aisle；轨道几何可进 Fw 扩展 |

---

## 6. 对实施的直接要求

1. M1 主数据：`Wms_Zone` + **`Wms_Layer`** + `Wms_Aisle` + `Wms_Location`，皆含 PackId。  
2. srm-wcs / shuttle-wcs 分配只读本包前缀行。  
3. 更新 wms/05 与 02 前缀表示例：层用 `Wms_Layer`，不再写死「层=Zone」。  

**最终判断：统一账本骨架可行且应用；用 `Wms_Layer` 补齐四向层级后，即可同时满足 srm-wcs 与 shuttle-wcs 的不同实现，无需按包复制三套账本表。**
