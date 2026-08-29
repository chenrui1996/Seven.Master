# design

设计大纲与方案草案（实现前评审用）。

| 文档 | 说明 |
|------|------|
| [2026-08-29-wms-wcs-pack-architecture.md](./2026-08-29-wms-wcs-pack-architecture.md) | 立库合并 WMS/WCS：可插拔 WCS 包 + 薄编排总线 + 外部适配 |
| [2026-08-29-wms-wcs-implementation-guide.md](./2026-08-29-wms-wcs-implementation-guide.md) | 实现大纲、系统结构、主流程、硬规则、开发与使用方式 |
| [2026-08-29-seven-simulator-design.md](./2026-08-29-seven-simulator-design.md) | Seven.Simulator：Features→地图→仿真→Promote 生产闭环 |
| [**wms/**](./wms/) | **LES2 WMS 流程 + 一仓多包前缀货位 + 迁入 Seven + Seven.App PDA** |
| [**srm-wcs/**](./srm-wcs/) | **堆垛机 WCS：LES2/LESWebVM 运行时 + Stk_ 包迁移** |
| [**shuttle-wcs/**](./shuttle-wcs/) | **四向 Shuttle：RCS4Shuttle 流程 + Fw_ 迁移 + 与 srm 统一主数据裁定** |
| [entity-cache-implementation-outline.md](./entity-cache-implementation-outline.md) | 物料、库存、容器、上下架任务、设备执行任务、路径、流量的缓存完整实现大纲 |

相关实现计划：[`docs/superpowers/plans/2026-08-29-wms-wcs-pack-implementation.md`](../docs/superpowers/plans/2026-08-29-wms-wcs-pack-implementation.md)  
模拟器工程：[`Seven.Simulator/`](../Seven.Simulator/)

### 2026-08-29 补充决策

- **一仓多包（B）**；库区/巷道/货位以 **WCS 类型前缀**区分；四向另有 **`Wms_Layer`**。见 [`wms/02`](./wms/02-location-multi-pack-prefix.md)、[`shuttle-wcs/03` 裁定](./shuttle-wcs/03-vs-srm-unified-wms-verdict.md)。
- 启用 WMS 的仓库须至少绑定一种 WCS；结构与分配策略在各包内。
- **统一 `Wms_*` 账本骨架可行**（RCS 已共表跑 SRM+Shuttle）；分配策略分 `Stk_*`/`Fw_*`，不按包复制三套账本。
