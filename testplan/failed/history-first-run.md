# 首轮 ApiHttp 失败留痕（已修复复测通过）

时间：2026-09-13 首跑 `run-api-cases.ps1`

| CaseId | Status | Detail | 根因 | 修复 |
|--------|--------|--------|------|------|
| TC-X-021 | FAIL | set manual fail | POST body `mode:"Manual"` 无法绑定 `WcsControlMode` 枚举 → HTTP 400 | 使用 `mode: 1`（Manual）；0=Auto |
| TC-OPS-STK-003 | FAIL | TargetType field is required | 误传 `kind`；契约为 `targetType` + `id` | `{"targetType":"device","id":"..."}` |

复测：同脚本第二轮 **pass=29 fail=0 skip=1**。
