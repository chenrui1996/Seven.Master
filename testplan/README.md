# testplan — 全流程测试计划与用例索引

状态：与 **2026-09-13 项目现状**同步（运维菜单/Ops API 已落地；服务级 E2E 187 通过；ApiHttp/Playwright 冒烟已跑）  
**最新执行报告**：[TEST-REPORT-2026-09-13.md](./TEST-REPORT-2026-09-13.md) · **未通过/未执行**：[failed/](./failed/)  
设计依据：[`../design/ops`](../design/ops)、[`../design/shuttle-wcs/04`](../design/shuttle-wcs/04-end-to-end-flow.md)、[`../design/srm-wcs/03`](../design/srm-wcs/03-end-to-end-and-ops.md)、[`../doc/20`](../doc/20-WMS与WCS实现说明.md)、[`../doc/24`](../doc/24-运维Ops与联锁.md)

---

## 1. 文档结构

| 文档 | 类型 | 说明 |
|------|------|------|
| [00-conventions.md](./00-conventions.md) | 约定 | 用例模板、优先级、自动化分层、钩子 ID 登记表 |
| [TEST-REPORT-2026-09-13.md](./TEST-REPORT-2026-09-13.md) | **报告** | 本次执行摘要 |
| [failed/](./failed/) | **未通过** | FAIL / SKIP / BLOCKED / 未跑闭环 |
| [wms-e2e.md](./wms-e2e.md) | 计划 | WMS 范围、环境、回归清单 |
| [cases/wms-cases.md](./cases/wms-cases.md) | **用例** | 入库/出库/盘点/PDA/接口日志 详细 TC |
| [srm-wcs-e2e.md](./srm-wcs-e2e.md) | 计划 | 立库分配/拆腿/运维 |
| [cases/srm-wcs-cases.md](./cases/srm-wcs-cases.md) | **用例** | SRM + Stacker Ops 详细 TC |
| [shuttle-wcs-e2e.md](./shuttle-wcs-e2e.md) | 计划 | 四向同层/跨层/运维 |
| [cases/shuttle-wcs-cases.md](./cases/shuttle-wcs-cases.md) | **用例** | FourWay + Ops 详细 TC |
| [cases/fourway-scheduling-singapore.md](./cases/fourway-scheduling-singapore.md) | **场景** | 新加坡 simproj：路径/交通/停车/Hoist |
| [cases/stacker-scheduling-srm-demo.md](./cases/stacker-scheduling-srm-demo.md) | **场景** | SRM Demo：双深/拆腿/限高 |
| [fixtures/](./fixtures/) | **地图** | simproj→Seven 转换产物 |
| [scripts/convert-simproj-to-seven-fixture.mjs](./scripts/convert-simproj-to-seven-fixture.mjs) | 脚本 | RCS `.simproj.json` → fixture |
| [cross-pack-closed-loop.md](./cross-pack-closed-loop.md) | 计划 | 多包交接、菜单 IA |
| [cases/cross-pack-cases.md](./cases/cross-pack-cases.md) | **用例** | 跨包 + 菜单/联锁 UI 详细 TC |
| [cases/discovered-2026-09-13.md](./cases/discovered-2026-09-13.md) | **补例** | 执行中发现的契约/认证用例 |
| [scripts/run-api-cases.ps1](./scripts/run-api-cases.ps1) | 脚本 | 活库 ApiHttp（只读/契约） |
| [retain-data-e2e.md](./retain-data-e2e.md) | **落库保留计划** | `RETAIN*` 写路径，测完不删 |
| [retain-locations-e2e.md](./retain-locations-e2e.md) | **库位落库保留** | 新加坡/SRM Demo 地图级 `Wms_Location` KEEP |
| [scripts/run-retain-cases.ps1](./scripts/run-retain-cases.ps1) | 脚本 | 落库保留执行 |
| [scripts/run-retain-locations.ps1](./scripts/run-retain-locations.ps1) | 脚本 | 地图库位灌库保留 |
| [cases/table-coverage-matrix.md](./cases/table-coverage-matrix.md) | **全表覆盖** | TC-COV-* 与缺口 |
| [scripts/run-full-coverage-cases.ps1](./scripts/run-full-coverage-cases.ps1) | 脚本 | 主数据+闭环造数 |
| [reports/coverage/](./reports/coverage/) | **报告** | 40 API 探针终态 |
| [reports/retain/](./reports/retain/) | **报告** | 最近一次落库保留结果 |
| [e2e/](./e2e/) | 脚本 | Playwright 浏览器冒烟 |

---

## 2. 项目现状（测试视角）

| 能力 | 现状 | 测试落点 |
|------|------|----------|
| WMS 入/出/盘点服务闭环 | 已实现 | `Seven.Tests`：`WmsOrderServiceTests`、`InboundTo*E2E`、`OutboundTo*E2E` |
| 立库分配/双深/寻路 | 已实现 | `Stacker*` / `InboundToStacker` / `OutboundToStacker` |
| 四向分配/停车/提升 | 已实现 | `FourWay*` / `InboundToFourWay` / `OutboundToFourWay` / `FourWayHoistE2E` |
| 编排总线 / 双包交接 | 已实现 | `OrchestrationBusTests`、`PackPrefixAndMultiPackTests` |
| 平台联锁 / Ifc 日志 | 已实现 | `PlatformIfcCtlTests`；菜单在**系统管理** |
| 立库/四向 Ops API + Vue 页 | **已落地** | ApiHttp 读路径+负例；Playwright 页面冒烟；**仍缺** `*Ops*Tests` |
| Vue `data-testid` | **未铺** | 用例中预留 `ui:*` 钩子 |
| 浏览器 E2E | **Playwright 脚手架已建** | `testplan/e2e` |

测试工程：[`Seven.Net8/Seven.Tests`](../Seven.Net8/Seven.Tests/) + `testplan/scripts` + `testplan/e2e`。

---

## 3. Features 组合矩阵

| 代号 | Features | 用途 |
|------|----------|------|
| WMS | `wms` | 平库账本、无设备 |
| STK | `wms` + `orchestrationBus` + `wcsPacks.stacker` | 立库 E2E |
| FW | `wms` + `orchestrationBus` + `wcsPacks.fourWay` | 四向 E2E |
| DUAL | STK ∪ FW | 跨包、静默、菜单双边 |
| OPS-UI | 同 DUAL/STK/FW | 运维菜单可见性、联锁 UI |

设备侧默认：`InMemoryEquipmentTriggerPort` + `POST /api/Wcs/Triggers/*`；可选 Seven.Simulator。

---

## 4. 全局通过准则

1. **账本守恒**：`Wms_Stock` 数量与 Container/Location 占用一致；包内无第二套可扣减库存。  
2. **总线干净**：失败 Leg 不推进后续；Complete 必触发 `WmsTransportCompletionHandler`；重复 Complete 不双记账。  
3. **前缀隔离**：`Stk.*` / `Fw.*` 不串包 `AcceptLeg`。  
4. **门控**：双空闲、出入互斥、急停拒派符合设计。  
5. **运维 IA**：无「执行运维」钉底；运维在包内；Ifc 在系统管理。  
6. **可重复**：空库种子 + 固定主数据可重跑。

---

## 5. 自动化分层（见 conventions）

| Layer 代号 | 含义 | 现状工具 |
|------------|------|----------|
| `DotNet` | xUnit 服务/包级 | `dotnet test`（建议 `-o testplan/reports/testbin` 避锁） |
| `ApiHttp` | 真实/TestServer HTTP | `scripts/run-api-cases.ps1`；Ops/WMS 写闭环仍待扩 |
| `Browser` | Playwright | `testplan/e2e` |
| `Manual` | 现场/设备 | — |

---

## 6. 推荐命令

```powershell
cd D:\Junheinrich\Junheinrich.Master\Seven.Master\Seven.Net8

# 全量（独立输出目录，避免锁 WebApi）
dotnet test Seven.Tests\Seven.Tests.csproj -o ..\testplan\reports\testbin `
  --logger "trx;LogFileName=dotnet-all.trx" --results-directory ..\testplan\reports\dotnet

# 过滤示例
dotnet test --filter "FullyQualifiedName~Wms|FullyQualifiedName~Pda|FullyQualifiedName~Stock"
dotnet test --filter "FullyQualifiedName~Stacker|FullyQualifiedName~InboundToStacker"
dotnet test --filter "FullyQualifiedName~FourWay|FullyQualifiedName~InboundToFourWay"
dotnet test --filter "FullyQualifiedName~Orchestration|FullyQualifiedName~Platform|FullyQualifiedName~PackPrefix"

# 活库 API + 浏览器
powershell -ExecutionPolicy Bypass -File ..\testplan\scripts\run-api-cases.ps1
powershell -ExecutionPolicy Bypass -File ..\testplan\scripts\run-retain-cases.ps1   # 落库并保留
powershell -ExecutionPolicy Bypass -File ..\testplan\scripts\run-full-coverage-cases.ps1  # 全表覆盖造数
cd ..\testplan\e2e; npx playwright test
```

落库前缀与验收：[`retain-data-e2e.md`](./retain-data-e2e.md)；全表矩阵：[`cases/table-coverage-matrix.md`](./cases/table-coverage-matrix.md)；操作指南：[`doc/25`](../doc/25-联调示例数据操作指南.md)；方法论文档：[`doc/keypoint/09`](../doc/keypoint/09-测试方法与落库回归.md)。

---

## 7. 发布门禁建议

| 门禁 | 覆盖 |
|------|------|
| P0 DotNet | 全部 ExistingTest 标注的 TC（当前 187） |
| P0 ApiHttp | 菜单 IA、Ops GET、control-mode 契约、登录 |
| P0 Browser | 登录、无执行运维、Ops 页可开 |
| P1 | 跨层提升、深浅 Transfer、Handover、PDA、Ops 写闭环 |
| 冒烟剧本 | [X-08](./cross-pack-closed-loop.md) A–E |
