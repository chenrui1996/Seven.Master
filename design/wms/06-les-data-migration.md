# LES 主数据与库存迁移（M6）

状态：已落地（脚本 + 前缀映射单测）  
日期：2026-08-29  
脚本目录：[`../../scripts/les-migrate/`](../../scripts/les-migrate/)

---

## 1. 目标

| 源（LES2） | 目标（Seven） | 要点 |
|------------|---------------|------|
| `Warehouse` + `WarehouseZone` | `Wms_Warehouse` | `EnabledPackIds` 由本仓库区类型汇总 |
| `Zone` | `Wms_Zone` | `PackId` + `Stk.Z-*` / `Fw.Z-*` |
| `Aisle` | `Wms_Aisle` | `Stk.A-*`；权重/深/EP 点 |
| `Location` | `Wms_Location` | `Stk.B-*` 或 `Fw.N-*`；坐标列保留 |
| `Container` / `ContainerType` | `Wms_Container*` | 容器码不变；`LocationCode` 转前缀 |
| `Stock` | `Wms_Stock` | 经容器定位货位；`Batch`→`Lot` |
| `AssignmentPolicy` | `Stk_AssignmentPolicy` | 巷道码已前缀化 |

**不迁**：业务单据、WCS 运行任务、四向路网几何（另迭代）。

---

## 2. 前缀（与 [02](./02-location-multi-pack-prefix.md) 一致）

代码权威：`Seven.Domain.Wms.LesMigrationCodes`。  
SQL 镜像：`dbo.fn_Mig_*`（`01-prefix-functions.sql`）。

| ZoneType（LES） | 默认 Pack |
|-----------------|-----------|
| Asrs / Floor / General / Shelving / … | `stacker` |
| DenseWarehouse(30) / MiniLoad(31) | 配置项 `DenseMiniLoadPackId`（默认 stacker，可改 fourway） |

---

## 3. 库存定位

LES `Stock` **无** `LocationCode`，靠 `ContainerCode` → `Container.LocationCode`。  
迁移时 JOIN 映射表；对不上的行在对账中列出，写入时标 `UNKNOWN`（验收应清空）。

---

## 4. 验收清单

- [ ] `dotnet test --filter LesMigrationCodesTests` 通过  
- [ ] DryRun 映射表抽样与设计表一致  
- [ ] 正式写入后 `05-policy-and-reconcile.sql`：行数/数量合计、无裸码、无 UNKNOWN  

---

## 5. 与平台迁移脚本关系

| 脚本 | 范围 |
|------|------|
| `scripts/migrate-data.sql` | Sys_User / Role / Menu（Legrand→Seven） |
| `scripts/les-migrate/*` | WMS 主数据与库存（LES→Seven） |
