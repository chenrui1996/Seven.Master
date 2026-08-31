# design/wms — WMS 流程与迁移

本目录承接 LES2 WMS 读懂结果、迁入 Seven 的完整方案，以及 PDA（Seven.App）规划。

| 文档 | 内容 |
|------|------|
| [01-les2-wms-flows.md](./01-les2-wms-flows.md) | LES2 主数据与入/出/盘点流程；StorageTask / RetrievalTask 交接 |
| [02-location-multi-pack-prefix.md](./02-location-multi-pack-prefix.md) | **决策 B**：一仓多包；库区/巷道/货位/层以 **WCS 类型前缀** 区分 |
| [05-masterdata-and-allocation-storage.md](./05-masterdata-and-allocation-storage.md) | **多层主数据与分配流水线如何落表**（含 `Wms_Layer`） |
| [03-migration-plan.md](./03-migration-plan.md) | 迁到本仓库（Seven.Net8 + Vue3）的完整方案与阶段 |
| [06-les-data-migration.md](./06-les-data-migration.md) | **M6**：LES 主数据+库存迁移脚本与前缀对账 |
| [../shuttle-wcs/03-vs-srm-unified-wms-verdict.md](../shuttle-wcs/03-vs-srm-unified-wms-verdict.md) | 与堆垛对照：统一 Wms_* 能否满足 |
| [04-seven-app-pda.md](./04-seven-app-pda.md) | LesApp → Seven.App（uni-app）迁移方案 |

关联：

- **产品文档（落地实现全文）**：[`../../doc/20-WMS与WCS实现说明.md`](../../doc/20-WMS与WCS实现说明.md)
- 架构总览：[`../2026-08-29-wms-wcs-pack-architecture.md`](../2026-08-29-wms-wcs-pack-architecture.md)
- 堆垛机 WCS：[`../srm-wcs/`](../srm-wcs/)
- 产品说明：[`../../doc/19-WMS与WCS包.md`](../../doc/19-WMS与WCS包.md)

**已确认约束（2026-08-29）**

1. 一仓可挂多个 WCS 包（方案 B）。
2. 库区 / 巷道 / 货位 / 层等位置数据以 **WCS 类型码为前缀** 区分（如 `Stk.` / `Fw.`）。
3. 位置结构细节与分配策略在各 WCS 包内；启用 WMS 须至少绑定一种 WCS，否则无货位语义。
4. Features 模块开关仅控前端菜单；后端表与业务常驻（见 `doc/14`）。
