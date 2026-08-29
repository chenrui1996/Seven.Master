# design/shuttle-wcs — 四向穿梭车（Shuttle）WCS

本目录基于 `D:\Junheinrich\Shuttle\RCS4Shuttle` 的 Shuttle WCS 读懂结果，给出迁入 Seven `Fw_`（fourway）包的方案，并与 [`../srm-wcs/`](../srm-wcs/) 对照，裁定统一 `Wms_*` 主数据是否够用。

| 文档 | 内容 |
|------|------|
| [01-rcs4shuttle-flows.md](./01-rcs4shuttle-flows.md) | 主数据层级、分配链、任务与设备流程 |
| [02-migration-plan.md](./02-migration-plan.md) | 迁入 Seven FourWay 包的完整方案 |
| [03-vs-srm-unified-wms-verdict.md](./03-vs-srm-unified-wms-verdict.md) | **与 srm-wcs 对比 + 统一 Wms_Zone/Aisle/Location 能否满足** |

源码根：`D:\Junheinrich\Shuttle\RCS4Shuttle`（不引程序集，只迁逻辑）。
