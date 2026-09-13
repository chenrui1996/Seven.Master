# design

设计大纲与方案草案（实现前评审用）。

| 文档 | 说明 |
|------|------|
| [2026-08-29-wms-wcs-pack-architecture.md](./2026-08-29-wms-wcs-pack-architecture.md) | 立库合并 WMS/WCS：可插拔 WCS 包 + 薄编排总线 + 外部适配 |
| [2026-08-29-wms-wcs-implementation-guide.md](./2026-08-29-wms-wcs-implementation-guide.md) | 实现大纲、系统结构、主流程、硬规则、开发与使用方式 |
| [2026-08-29-seven-simulator-design.md](./2026-08-29-seven-simulator-design.md) | Seven.Simulator：Features→地图→仿真→Promote 生产闭环 |
| [**ops/**](./ops/) | **运维 IA：取消「执行运维」，下沉到立库/四向 WCS 运维菜单** |
| [**wms/**](./wms/) | **LES2 WMS 流程 + 一仓多包前缀货位 + 迁入 Seven + Seven.App PDA** |
| [**srm-wcs/**](./srm-wcs/) | **堆垛机 WCS：运行时 + Stk_ 迁移 + 全流程与运维** |
| [**shuttle-wcs/**](./shuttle-wcs/) | **四向 Shuttle：RCS 流程 + Fw_ 迁移 + 端到端闭环 + 运维规格** |
| [entity-cache-implementation-outline.md](./entity-cache-implementation-outline.md) | 物料、库存、容器、上下架任务、设备执行任务、路径、流量的缓存完整实现大纲 |

相关实现计划：[`docs/superpowers/plans/2026-08-29-wms-wcs-pack-implementation.md`](../docs/superpowers/plans/2026-08-29-wms-wcs-pack-implementation.md)  
全流程测试方案：[`../testplan/`](../testplan/)  
模拟器工程：[`Seven.Simulator/`](../Seven.Simulator/)

### 2026-08-29 补充决策

- **一仓多包（B）**；库区/巷道/货位以 **WCS 类型前缀**区分；四向另有 **`Wms_Layer`**。见 [`wms/02`](./wms/02-location-multi-pack-prefix.md)、[`shuttle-wcs/03` 裁定](./shuttle-wcs/03-vs-srm-unified-wms-verdict.md)。
- 启用 WMS 的仓库须至少绑定一种 WCS；结构与分配策略在各包内。
- **统一 `Wms_*` 账本骨架可行**（RCS 已共表跑 SRM+Shuttle）；分配策略分 `Stk_*`/`Fw_*`，不按包复制三套账本。

### 2026-09-12 运维与全流程决策

- **取消**顶级「执行运维」/`WcsOpsFolder`；**接口日志**迁系统管理；**2D 监控**并入各包运维监控；**联锁**下沉各包运维（仓级急停共享写权威）。见 [`ops/01`](./ops/01-ops-menu-restructure.md)。
- **四向完整闭环**与运维五页规格：[`shuttle-wcs/04`](./shuttle-wcs/04-end-to-end-flow.md)、[`05`](./shuttle-wcs/05-ops-menu-and-process.md)（对齐 RCS4Shuttle 运维四段式）。
- **立库全流程 + 运维四页**：[`srm-wcs/03`](./srm-wcs/03-end-to-end-and-ops.md)。
- 本阶段**仅文档**；菜单种子/Vue/API 实现另开任务。测试方案见 [`../testplan/`](../testplan/)。
