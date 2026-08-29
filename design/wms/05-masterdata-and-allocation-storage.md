# 多层主数据与分配策略存放（分包）

状态：方案补充（回答「不同 WCS 层级如何存、分配链如何存」）  
日期：2026-08-29  
前提：一仓多包 + 前缀码（[02](./02-location-multi-pack-prefix.md)）  

---

## 1. 原则（一句话）

| 内容 | 放哪 | 不放哪 |
|------|------|--------|
| **能挂库存的货位身份**（Code、占用、仓归属） | **`Wms_*` 通用表**（带 `PackId` + 前缀码） | 禁止包内第二套库存数量账本 |
| **层级怎么解释**（区/巷/层/位谁是谁） | 包的 **`IWcsLocationSchema`** + 可选包扩展表 | 不在 WMS 写死「层=库区」 |
| **分配流水线与策略参数**（巷→位，或层→巷→位） | **各包表 `Stk_*` / `Fw_*`** | 不进 `Wms_*`，不进 Bus |

---

## 2. 多层主数据如何存放

### 2.1 推荐模型：WMS 骨架表 + 包扩展（不要按包拆多套 Zone 表）

同一套物理表，用 **`PackId` 分区行**；不同 WCS **用到的层级不同**，空列允许为 null。

```text
Wms_Warehouse
  └── Wms_Zone            (PackId, Code=Stk.Z-* | Fw.Z-*)
        └── Wms_Layer       (PackId, Code=Fw.L-*)   // 四向必有；堆垛不建行
              └── Wms_Aisle   (PackId, Code=Stk.A-* | Fw.A-*)
                    └── Wms_Location (PackId, Code=Stk.B-* | Fw.N-*)
```

| 层级口语 | 堆垛机 (stacker) | 四向车 (fourway) | 落表 |
|----------|------------------|------------------|------|
| 库区 | `Stk.Z-ASRS01` | `Fw.Z-ASRS`（业务库区） | **`Wms_Zone`** |
| 层 | 无 Layer 主数据；货架层号在 Location.Layer | **`Fw.L02` 楼层主数据** | **`Wms_Layer`**（仅 Fw） |
| 巷道 | `Stk.A-01`（必填） | `Fw.A-L02-03`（属某层） | **`Wms_Aisle`** |
| 货位/节点 | `Stk.B-排-列-层-深` | `Fw.N-xxxx` | **`Wms_Location`** |

> **裁定（对照 RCS4Shuttle）**：不要把四向「层」当成 Zone。RCS 是 Zone→**Layer**→Aisle→Location，且与 SRM **共用同一套 BasicData 表**、两套分配引擎。详见 [`../shuttle-wcs/03-vs-srm-unified-wms-verdict.md`](../shuttle-wcs/03-vs-srm-unified-wms-verdict.md)。

**同仓两包示例行：**

| 表 | Code | PackId | 说明 |
|----|------|--------|------|
| Zone | `Stk.Z-ASRS` | stacker | 堆垛库区 |
| Aisle | `Stk.A-01` | stacker | 巷道 1 |
| Location | `Stk.B-01-02-03-1` | stacker | 货位 |
| Zone | `Fw.Z-ASRS` | fourway | 四向库区 |
| Layer | `Fw.L02` | fourway | 二层 |
| Aisle | `Fw.A-L02-01` | fourway | 二层巷 1 |
| Location | `Fw.N-1205` | fourway | 节点/货位 |

### 2.2 包专有属性（扩展表，按 Code/Id 挂靠）

通用表只留跨包都能理解的列；包差异进扩展，避免 `Wms_Location` 变成万金油。

| 包 | 扩展表示例 | 内容 |
|----|------------|------|
| Stacker | `Stk_AisleProfile` | EpPoint、MaxDepth、AllcationWeight、只入只出 |
| Stacker | `Stk_LocationProfile` | 双深 BinCode 组、ABC、与设备地址旁路（或 DeviceCoder） |
| FourWay | `Fw_MapVersion` + `Fw_Node` | 节点几何、所属层地图；`LocationCode`→`Fw.N-*` |
| FourWay | `Fw_Route` | 边；**不**替代 Wms_Location |

库存仍只认：`Wms_Stock.LocationCode` → 某条 `Wms_Location.Code`。

### 2.3 为何不「每包一套完整主数据表」

- 账本、盘点、PDA、交接位都要 **统一查库存/货位**。
- 前缀 + PackId 已隔离；再复制 `Stk_Zone`/`Fw_Zone` 会导致对账双源。
- 层级差异用 **Schema 解释 + 可空层级 + 扩展表** 解决即可。

### 2.4 Schema 声明（代码，非表）

```text
StackerSchema.Hierarchy = [ Zone, Aisle, Location(Row,Col,Layer,Depth) ]
FourWaySchema.Hierarchy = [ Zone, Layer, Aisle, Location(=Node) ]
```

UI/校验：建档时按当前 Pack 动态显示表单（堆垛要巷道；四向要层/地图节点）。

---

## 3. 分配策略如何存放

### 3.1 两层概念

1. **分配流水线（Pipeline）**：有序阶段列表——堆垛 `巷道→货位`；四向 `层→巷道→货位(节点)`。  
2. **阶段策略数据（Policy）**：每个阶段的规则参数、轮转链、候选过滤。

流水线 **定义在包代码**（或包内一小张配置表）；**策略参数一定落在包表**。

### 3.2 堆垛机：巷道分配 → 货位分配

```text
Stk_AllocationStage（可选配置，默认代码写死两级）
  1 = Aisle
  2 = Location

Stk_AssignmentPolicy          // 巷道级：高/重上限、Next 正反向、AisleCode、Classify…
Stk_AssignmentRecord          // 运行时轮转进度
（货位级）规则可配置表或代码 + 读 Wms_Location/Stk_LocationProfile
  SelectLocation：双深、Booking、ABC…
```

运行：`IWcsLocationAllocator(stacker).AllocateInbound`  
→ Stage1 写候选巷道/EpPoint → Stage2 写 `Wms_Location` 预约（`Stk.B-*`）。

### 3.3 四向车：层分配 → 巷道分配 → 货位分配

```text
Fw_AllocationStage
  1 = Layer      // 选 Wms_Layer（Fw.L*）
  2 = Aisle      // 层内 Wms_Aisle
  3 = Location   // Wms_Location / Fw_Node

Fw_LayerPolicy                 // 层负荷、是否开放、轮转（对齐 RCS AssignmentPolicy.LayerCode）
Fw_AislePolicy（可选）
Fw_NodePolicy / 选点规则       // 距离、拥堵、HotStore 占用
```

运行：`IWcsLocationAllocator(fourway)` 按三阶段调用；**绝不读 `Stk_AssignmentPolicy`**。

### 3.4 统一入口（WMS / PDA 只调接口）

```text
IWcsLocationAllocatorResolver.Get(packId) → IWcsLocationAllocator
AllocateInbound(ctx) → AllocationResult { stageResults[], finalLocationCode }
```

`ctx` 含：WarehouseId、容器高重、物料、可选指定层/巷。  
WMS **没有** `Wms_AssignmentPolicy` 总表。

### 3.5 策略与主数据的引用关系

```text
Stk_AssignmentPolicy.AisleCode ──► Wms_Aisle.Code (Stk.A-*)
货位结果 LocationCode ──────────► Wms_Location.Code (Stk.B-*)

Fw_LayerPolicy.LayerCode ───────► Wms_Layer.Code (Fw.L*)
Fw 巷道 / 最终节点 ─────────────► Wms_Aisle / Wms_Location (Fw.*)
```

禁止策略表存无前缀裸码。

---

## 4. 对照表（便于评审）

| 问题 | 答案 |
|------|------|
| 多层主数据几套库？ | **一套** `Wms_Zone` / **`Wms_Layer`** / `Wms_Aisle` / `Wms_Location`，按 PackId + 前缀分行 |
| 四向「层」存哪？ | **`Wms_Layer`（`Fw.L*`）**，隶属 `Wms_Zone`；不是 Zone 本身 |
| 堆垛「巷道」存哪？ | **`Wms_Aisle`（`Stk.A*`）** + `Stk_AisleProfile` |
| 分配链配置存哪？ | **包内** Stage + `Stk_*Policy` / `Fw_*Policy` |
| 能否一张策略表用 Type 区分包？ | **不推荐**；阶段数与字段不同，分前缀表更清晰 |
| 总线管分配吗？ | **不管**；只运最终 From/To LocationCode |
| 统一骨架够不够？ | **够**（RCS 实证）；详见 [shuttle-wcs/03](../shuttle-wcs/03-vs-srm-unified-wms-verdict.md) |

---

## 5. 迁入 LES 时注意

- LES `AssignmentPolicy` → **`Stk_AssignmentPolicy`**（仅堆垛）。  
- 四向若现网无独立「层策略」表，迁移时从地图/层主数据生成 `Fw_LayerPolicy` 初值。  
- 原 Zone/Aisle/Location 一律加前缀后再挂策略外键。

---

## 6. 实施切片（并入 wms M1 / srm P0）

1. 补 `Wms_Aisle` + 各表 `PackId`。  
2. 落地 `Stk_AssignmentPolicy/Record`（已有雏形则对齐 LES 字段）。  
3. 新增 `Fw_*Policy`（可与四向地图同迭代）。  
4. `IWcsLocationAllocator` 每包实现流水线；单测金样例分 Pack 隔离。
