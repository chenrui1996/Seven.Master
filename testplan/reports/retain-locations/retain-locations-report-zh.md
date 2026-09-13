# 库位落库保留报告

- RunId: **LOC-20260913-224447**
- 时间: 2026-09-13 22:44:58
- BaseUrl: http://localhost:5000
- 通过: **9** / 失败: **0** / 跳过: **1**（收货库存可见性，不影响库位）
- **数据策略: 保留（不清理）**
- 库位总数: **7（跑前历史）→ 271（中断续跑）→ 353（终态）**
- 分 Pack：`stacker=326` · `fourway=27`
- 详报（英文表）: [`retain-locations-report-20260913-224447.md`](./retain-locations-report-20260913-224447.md)
- 产物 JSON: [`retain-locations-artifacts-20260913-224447.json`](./retain-locations-artifacts-20260913-224447.json)

## 为何之前「库位没了」

| 先前脚本 | 实际写入库位数 |
|----------|----------------|
| `run-retain-cases.ps1` | 2（`Stk.RETAIN.*`） |
| `run-full-coverage-cases.ps1` | ~5（`Stk.RECV/LOC` + `Fw.*`） |
| DotNet InMemory | **不落 MySQL** |

地图级货位（SRM Demo 320 + 新加坡 Fw 槽位）此前只生成了 `testplan/fixtures/*.json`，**没有灌进活库**。本计划补上。

## UI 检索（localhost:5173）

1. **仓储WMS → 库位** 搜 `Stk.l` → 应见大量 Demo 货位（含 depth 1/2）
2. 搜 `Fw.` → 新加坡子轨货位 + `Fw.RECV-MAP`
3. 搜 `Stk.RECV-MAP`
4. **巷道** 搜 `Stk.A` → A1..A4；搜 `Fw.A`
5. **层** 搜 `Fw.L01` / `Fw.L02`

抽样码：`Stk.l101011`、`Stk.l101012`（浅/深成对）、`Fw.SW1_1Z001A0505_0505_0504_01.S1`

## 用例结果

| CaseId | Status | 说明 |
|--------|--------|------|
| TC-LOC-000 | PASS | 登录 |
| TC-LOC-001 | PASS | 仓 `RETAIN_WH` |
| TC-LOC-010 | PASS | Zone/Aisle×4 |
| TC-LOC-011 | PASS | SRM Demo 320 货位（本跑补齐剩余 57） |
| TC-LOC-012 | PASS | depth1=160 / depth2=160 |
| TC-LOC-020 | PASS | 四向 Zone/Layer/Aisle |
| TC-LOC-021 | PASS | 新加坡 Location 节点 24 |
| TC-LOC-030 | PASS | total 353 ≥ 基线 |
| TC-LOC-031 | SKIP | 同址收货状态机提示（库位已在） |
| TC-LOC-040 | PASS | 本报告 |

## 复跑命令

```powershell
powershell -ExecutionPolicy Bypass -File Seven.Master\testplan\scripts\run-retain-locations.ps1
# 幂等：已存在码跳过；含 429 重试与节流
```

计划文档：[`../retain-locations-e2e.md`](../retain-locations-e2e.md)
