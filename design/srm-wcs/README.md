# design/srm-wcs — 堆垛机（SRM）WCS 包

本目录合并 **LES2 Models/WCS + Storage/Retrieval 交接** 与 **LESWebVM（申请/分配/路径拆腿）** 的读懂结果，给出可落入 Seven `Stk_` 包的迁移方案与运维设计。

| 文档 | 内容 |
|------|------|
| [01-runtime-from-les.md](./01-runtime-from-les.md) | LES2 + LESWebVM 运行时对照（SUDR→分配→运输→拆腿） |
| [02-migration-plan.md](./02-migration-plan.md) | Seven Stacker 包落地切分、阶段与前缀货位 |
| [03-end-to-end-and-ops.md](./03-end-to-end-and-ops.md) | **立库端到端流程 + 运维菜单/页面规格** |

关联：

- 运维 IA：[`../ops/01-ops-menu-restructure.md`](../ops/01-ops-menu-restructure.md)
- 货位前缀与一仓多包：[`../wms/02-location-multi-pack-prefix.md`](../wms/02-location-multi-pack-prefix.md)
- WMS 交接：[`../wms/01-les2-wms-flows.md`](../wms/01-les2-wms-flows.md)
- 包架构：[`../2026-08-29-wms-wcs-pack-architecture.md`](../2026-08-29-wms-wcs-pack-architecture.md)
- 测试：[`../../testplan/srm-wcs-e2e.md`](../../testplan/srm-wcs-e2e.md)

**范围边界**

- **纳入**：申请点、DestinationRequest、巷道/货位分配、PutOn/PutAway、PutOff/Retrieval、包内路径/流量/DeviceTask、SRM 执行语义、运维监控/堆垛/申请点/联锁。
- **包内二期或并行模块**：纯输送线编排可与 SRM 同包但分层；**不**把路径流量放进 Orchestration Bus。
- **不纳入本目录**：四向车 Fw_ 路网（另包）、外部供应商 Codec。
