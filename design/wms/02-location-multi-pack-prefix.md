# 一仓多包 + 位置数据 WCS 前缀（决策 B）

状态：**已确认**  
日期：2026-08-29  

---

## 1. 决策

| 项 | 选择 |
|----|------|
| 仓与包关系 | **一仓可挂多个 WCS 包**（Stacker / FourWay / External…） |
| 位置归属 | 每个库区/巷道/货位/层节点 **只属于一个 PackId** |
| 区分方式 | **编码前缀 = WCS 类型**（用户确认） |
| WMS 前提 | 启用 WMS 的仓库 **至少绑定一种 WCS**；否则禁止维护储位结构（缺货位语义） |
| 结构与分配 | **坐标 schema + 分配策略** 在各 WCS 包模块；WMS 只存通用账本字段 + PackId + 前缀码 |

---

## 2. 前缀约定

### 2.1 Pack 类型码（稳定短码）

| PackId | 类型前缀 | 典型层级口语 |
|--------|----------|--------------|
| `stacker` | `Stk` | 库区 → 巷道 → 位置（排/列/层/深） |
| `fourway` | `Fw` | 库区 → **层** → 巷道 → 货位/节点 |
| `boxsort` | `Bs` | （预留）线体/格口 |
| `external:{vendor}` | `Ext` 或供应商短码 | 外部系统映射码 |

### 2.2 编码格式

统一：

```text
{PackPrefix}.{业务段}
```

示例：

| 实体 | Stacker 示例 | FourWay 示例 |
|------|--------------|--------------|
| Zone | `Stk.Z-ASRS01` | `Fw.Z-ASRS` |
| Layer | （不用） | `Fw.L02` |
| Aisle | `Stk.A-01` | `Fw.A-L02-03` |
| Location / Node | `Stk.B-01-02-03-1` | `Fw.N-1205` |

规则：

1. **全局唯一**：`Code` 在全库唯一（前缀已隔离包）。
2. **PackId 冗余列**：`Wms_Zone` / `Wms_Layer` / `Wms_Aisle` / `Wms_Location` 必须有 `PackId`，禁止只靠解析前缀。
3. **禁止跨包复用无前缀码**：旧 LES 裸码迁入时由迁移工具加前缀。
4. **交接位**：`IsHandover=true` 的 Location 仍带所属包前缀；`Wms_HandoverLink` 描述 FromPack→ToPack。
5. **层**：四向用独立 `Wms_Layer`；堆垛货架层号仍用 `Wms_Location.Layer` 整型坐标（≠ Layer 主数据）。详见 [`../shuttle-wcs/03-vs-srm-unified-wms-verdict.md`](../shuttle-wcs/03-vs-srm-unified-wms-verdict.md)。

---

## 3. 数据落表（Seven）

### 3.1 WMS 通用层（权威账本）

| 表 | 新增/强化 | 说明 |
|----|-----------|------|
| `Wms_Warehouse` | `EnabledPackIds`（JSON/子表） | 本仓启用的包列表；≥1 |
| `Wms_Zone` | `PackId`, `Code` 前缀约束 | 结构类型由包解释 |
| `Wms_Layer`（补） | `PackId`, `Code`；隶属 Zone | **四向楼层**；堆垛不建行 |
| `Wms_Aisle` | `PackId`, `Code`, 可选 LayerId | 堆垛/四向巷道 |
| `Wms_Location` | `PackId`, `Code`；坐标列可空 | 四向可挂 Fw_Node；堆垛填排列层深 |
| `Wms_Stock` | `LocationCode`（建议直存） | 查询不必经容器；容器仍保留 |

WMS **不**解释「层是否等于库区」——由 `PackId` 对应包的 **LocationSchema** 解释。

### 3.2 包内结构扩展（非第二套库存账本）

| 包 | 表/概念 | 职责 |
|----|---------|------|
| Stacker | `Stk_RequestPoint`, `Stk_AssignmentPolicy`, `Stk_DeviceCoder`… | 申请点、轮转策略、设备地址映射；引用 `Stk.*` LocationCode |
| FourWay | `Fw_MapVersion`, `Fw_Node`, `Fw_Route`… | 层路网；`Fw_Node.LocationCode` → `Fw.*` |
| External | `Ext_*` 映射 | 外部码 ↔ 我方前缀码 |

**禁止**包内再维护一套可扣减库存数量账本；占用/预约标志可写在 `Wms_Location` 或包内运行时表，最终库存数量只认 `Wms_Stock`。

---

## 4. LocationSchema 插件（包内）

每个 WCS 包实现：

```text
IWcsLocationSchema
  PackId
  ValidateZone/Aisle/Location(dto)   // 坐标完整性
  NormalizeCode(...)                 // 强制前缀
  DescribeHierarchy()                // 堆垛=区-巷-位；四向=区-层-巷-位
```

```text
IWcsLocationAllocator（包内）
  AllocateInbound(...)               // 堆垛巷→位；四向层→巷→位
  AllocateOutboundHint(...)          // 可选
```

WMS 收货选区、上架推荐：**只调用目标 Pack 的 Allocator**，不在 WMS 写死轮转算法。

---

## 5. 启用规则（产品）

1. 创建/启用仓库时：向导选择 **至少一个** Pack；写入 `EnabledPackIds`。
2. Features 菜单：Wms 显示仍要勾选；同时至少一种 `WcsPacks.*` 或 External 配置，否则 UI 提示「无货位结构」。
3. 后端校验：`CreateLocation` / Deploy 地图 时 `PackId ∈ Warehouse.EnabledPackIds`，且 Code 前缀匹配该 Pack。
4. 跨包搬运：仅通过 `Bus_TransportOrder` + `Wms_HandoverLink`；各包只认自己前缀的码。

---

## 6. 与旧架构文案的关系

- 原「`Wms_Location` 唯一权威、包只引用 Code」**仍然成立**。
- 补充：**Code 带包前缀 + PackId 列**；一仓多包时靠此前缀与交接位协作；四向补 **`Wms_Layer`**。
- 原「路径/流量不跨包」不变。

---

## 7. 迁移 LES / RCS 裸码示例

| 源 | Seven |
|-----|-------|
| Zone `ASRS01`（堆垛） | `Stk.Z-ASRS01` |
| Aisle `A01` | `Stk.A-A01` |
| Location `0102031` | `Stk.B-0102031` |
| Zone（四向） | `Fw.Z-ASRS` |
| Layer `L2` | `Fw.L02` |
| 四向节点 `N1205` | `Fw.N-1205` |
