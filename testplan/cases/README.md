# cases — 详细用例目录

| 文件 | 域 | 约计用例 |
|------|-----|----------|
| [wms-cases.md](./wms-cases.md) | WMS / PDA / Ifc 菜单 | TC-WMS-001～051 |
| [srm-wcs-cases.md](./srm-wcs-cases.md) | 立库 + Stacker Ops | TC-SRM-* / TC-OPS-STK-* |
| [shuttle-wcs-cases.md](./shuttle-wcs-cases.md) | 四向 + FourWay Ops | TC-FW-* / TC-OPS-FW-* |
| [cross-pack-cases.md](./cross-pack-cases.md) | 跨包 / 菜单 IA / 冒烟 | TC-X-* |
| [discovered-2026-09-13.md](./discovered-2026-09-13.md) | 执行中补例 | TC-AUTH-* / 契约 |
| [table-coverage-matrix.md](./table-coverage-matrix.md) | **全表覆盖矩阵** | TC-COV-* |

字段与钩子：[`../00-conventions.md`](../00-conventions.md)。  
报告：[`../TEST-REPORT-2026-09-13.md`](../TEST-REPORT-2026-09-13.md) · 覆盖：[`../reports/coverage/`](../reports/coverage/) · 未通过：[`../failed/`](../failed/)。  
操作指南：[`../../doc/25-联调示例数据操作指南.md`](../../doc/25-联调示例数据操作指南.md)。

## 自动化落地优先级（脚本 backlog）

| 序 | FutureTest / 工程 | 覆盖 Hook 前缀 | 对应用例 |
|----|-------------------|----------------|----------|
| 1 | `StackerOpsServiceTests` | `hook:stk.ops.*` | TC-OPS-STK-001～007 |
| 2 | `FourWayOpsServiceTests` | `hook:fw.ops.*` | TC-OPS-FW-001～013 |
| 3 | `*OpsApiTests` (WebApplicationFactory) | 同上 + Permission | 同上 P0 |
| 4 | `WmsHttpE2ETests` | `hook:wms.*` | TC-WMS-001/013/022 |
| 5 | `DbSeederOpsMenuTests` | `hook:sys.menu` | TC-X-040 |
| 6 | Playwright `e2e/ops-menu.spec.ts` | `hook:ui.menu.*` | TC-X-010～012, TC-OPS-FW-020 |
| 7 | Playwright `e2e/wcs-ops-*.spec.ts` | `hook:ui.fw.ops.*` | TC-OPS-FW-021～022, TC-OPS-STK-008 |
| 8 | Vue 补齐 `data-testid` | `UiTestId` 列 | 全部 Browser 案 |

每个新测试类请加：

```csharp
[Trait("CaseId", "TC-OPS-FW-010")]
[Trait("Hook", "hook:fw.ops.forceComplete")]
[Trait("Priority", "P0")]
```
