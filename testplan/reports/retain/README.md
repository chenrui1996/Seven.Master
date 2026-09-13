# 落库保留 — 执行记录（2026-09-13）

- **RunId**：`RETAIN-20260913-195525`
- **脚本**：`scripts/run-retain-cases.ps1`
- **结果**：PASS 9 / FAIL 0 / SKIP 1（四向运维入库无 gateway）
- **Browser**：`retain-ui.spec.ts` PASS（TC-RETAIN-008）
- **详细**：[`retain-report.md`](./retain-report.md) · [`retain-artifacts-20260913-195525.json`](./retain-artifacts-20260913-195525.json)

## 库内可见数据（已保留）

| 类型 | 关键值 |
|------|--------|
| 仓库 | `RETAIN_WH` |
| 库位 | `Stk.RETAIN.RECV-01`、`Stk.RETAIN.STK-A` |
| 入库 | `RETAIN-IN-20260913-195525-01`（Completed）、`-02` |
| 出库 | `RETAIN-OUT-20260913-195525-01`（Ship 10） |
| 盘点 | `RETAIN-CC-20260913-195525-01`（录 25） |
| 库存 | `RETAIN-TP-…-02` @ RECV **qty=25**；`…-01` qty=0（已出完） |
| 申请点 | `RETAIN.RP-01` |

## 过程中修复

1. 库位须 `Stk.`/`Fw.` 前缀或 `packId`  
2. 单据 `add` 响应 JSON 环 → 脚本按单号回查；代码已加 `IgnoreCycles`/`JsonIgnore`（**重启 WebApi 后生效**）
