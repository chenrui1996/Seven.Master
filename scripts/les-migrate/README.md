# LES2 → Seven WMS 数据迁移（M6）

把 LES **主数据 + 库存**迁入 Seven，并给库区/巷道/货位加 **Pack 前缀码**（`Stk.*` / `Fw.*`）。

设计说明见 [`../../design/wms/06-les-data-migration.md`](../../design/wms/06-les-data-migration.md)。  
前缀金样例与 C# 映射：`Seven.Domain.Wms.LesMigrationCodes`（单测 `LesMigrationCodesTests`）。

## 前置

1. SevenDb 已执行 EF 迁移（存在 `Wms_*`、`Stk_AssignmentPolicy`）。
2. SQL Server **同实例**可访问 LES 库（跨库三部分名）。
3. LES 业务表名与实体一致：`Warehouse`、`WarehouseZone`、`Zone`、`Aisle`、`Location`、`Container`、`ContainerType`、`Stock`、`AssignmentPolicy`。
4. 建议在 **业务停机 / 只读** 窗口执行；先 DryRun。

## 执行顺序

| 步 | 脚本 | 作用 |
|----|------|------|
| 0 | `00-config.sql` | 写 `Mig_LesRunConfig`（库名、仓码、Dense→Pack、DryRun） |
| 1 | `01-prefix-functions.sql` | 前缀 UDF + `Mig_LesCodeMap` |
| 2 | `02-build-code-map.sql` | 从 LES 填映射表（可反复跑） |
| 3 | `03-masterdata.sql` | Warehouse / Zone / Aisle / Location |
| 4 | `04-containers-stock.sql` | ContainerType / Container / Stock |
| 5 | `05-policy-and-reconcile.sql` | `Stk_AssignmentPolicy` + 对账 |

```sql
-- 改配置后
UPDATE dbo.Mig_LesRunConfig SET DryRunOnly = 0, WarehouseCode = N'你的仓码';
-- 再从 02 或 03 重跑
```

## 前缀规则（摘要）

| LES | Pack | Seven |
|-----|------|-------|
| Zone `ASRS01` | stacker | `Stk.Z-ASRS01` |
| Aisle `A01` | stacker | `Stk.A-A01` |
| Location `0102031` | stacker | `Stk.B-0102031` |
| Location 节点 | fourway | `Fw.N-1205` |
| Layer `L2` | fourway | `Fw.L02` |

- `ZoneType.Asrs/Floor/…` → 默认 `stacker`
- `DenseWarehouse(30)` / `MiniLoad(31)` → `DenseMiniLoadPackId`（默认 `stacker`，四向站改 `fourway`）
- 库存：`Stock` ⋈ `Container.LocationCode` → 映射表 → `Wms_Stock.LocationCode`

## 验收

1. `LesMigrationCodesTests` 全部通过。
2. `05` 对账：Zone/Aisle/Location 行数与映射表一致；`Stock` 数量合计接近。
3. 无 `LocationCode = UNKNOWN`；`Wms_*` Code 无裸码。
4. 抽 10 条货位，对照 LES 原码与前缀码。

## 不迁（本包范围外）

- 入/出库单据、StorageTask/RetrievalTask、运行中设备任务  
- 系统用户（见根目录 `scripts/migrate-data.sql`）  
- 四向 `Fw_Node` 地图几何（有 RCS 库时另脚本）

## 回滚提示

未提供自动 DROP 全量脚本。测试库可：

```sql
-- 慎用：按仓清理（示例）
DELETE FROM dbo.Wms_Stock;
DELETE FROM dbo.Wms_Container;
DELETE FROM dbo.Wms_Location;
DELETE FROM dbo.Wms_Aisle;
DELETE FROM dbo.Wms_Zone;
DELETE FROM dbo.Wms_Warehouse WHERE Code = N'WH01';
DELETE FROM dbo.Stk_AssignmentPolicy;
TRUNCATE TABLE dbo.Mig_LesCodeMap;
```
