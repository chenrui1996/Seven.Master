# failed-cases.md — 当前未通过/未执行清单

更新日期：2026-09-13（全表覆盖轮之后）

| CaseId | Status | Layer | 说明 |
|--------|--------|-------|------|
| TC-COV-STK-IN | PARTIAL | ApiHttp | 首轮规划报 `no pack or handover`；后续靠交接链/强制完成与 SQL 演示行补齐任务表 |
| TC-COV-OPT-DC | SKIP | ApiHttp | `Features.deviceComm=false` |
| 跨层 Hoist 真闭环 | WEAK | Live | `Fw_Hoist*` 有演示行；真三阶段仍靠 DotNet `FourWayHoistE2E` |
| Chrome MCP UI | BLOCKED | Infra | 已用 Playwright 替代 |

**表探针**：常用 40 个业务 API **全部非空**（见 `reports/coverage/table-snapshot-final.json`）。

历史：首轮契约失败见 [history-first-run.md](./history-first-run.md)；RETAIN 轮见 `reports/retain/`。
