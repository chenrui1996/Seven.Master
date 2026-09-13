# 落库保留测试报告

- RunId: **RETAIN-20260913-195525**
- 时间: 2026-09-13 19:55:29
- BaseUrl: http://localhost:5000
- 通过: **9** / 失败: **0** / 跳过: **1**
- 产物 JSON: `reports/retain/retain-artifacts-20260913-195525.json`
- **数据策略: 保留**（前缀 `RETAIN_` / `RETAIN-` / `RETAIN.`）

## 如何在 UI 查找

1. 仓库 `RETAIN_WH`
2. 库位 `Stk.RETAIN.RECV-01` / `Stk.RETAIN.STK-A`
3. 入库 `RETAIN-IN-20260913-195525-01` / `RETAIN-IN-20260913-195525-02`
4. 出库 `RETAIN-OUT-20260913-195525-01`
5. 盘点 `RETAIN-CC-20260913-195525-01`
6. 库存物料 `RETAIN-MAT-01` 容器 `RETAIN-TP-20260913-195525-02`（盘点后应为 25）
7. 申请点 `RETAIN.RP-01`

| CaseId | Status | Title | Detail |
|--------|--------|-------|--------|
| TC-RETAIN-000 | PASS | Login for retain run | RETAIN-20260913-195525 |
| TC-RETAIN-001 | PASS | Seed warehouse + locations | whId=1 recv=1 stkA=2 |
| TC-RETAIN-002 | PASS | Inbound same-loc complete (KEEP) | id=4 status=Completed container=RETAIN-TP-20260913-195525-01 |
| TC-RETAIN-003 | PASS | Second inbound pallet (KEEP) | id=5 qty=20 RETAIN-TP-20260913-195525-02 |
| TC-RETAIN-004 | PASS | Outbound ship (KEEP order) | id=2 shipped 10 from RETAIN-TP-20260913-195525-01 |
| TC-RETAIN-005 | PASS | Cycle count adjust KEEP | id=2 countQty=25 on RETAIN-TP-20260913-195525-02 |
| TC-RETAIN-006 | PASS | Request point seed + Ops toggle KEEP | id=1 |
| TC-RETAIN-007 | PASS | Menu IA no legacy ops | ok |
| TC-RETAIN-009 | SKIP | FourWay ops inbound (optional) | no gateways/layers in live DB |
| TC-RETAIN-010 | PASS | Stock snapshot retained | rows=2 |
