# 落库并保留 — 活库写路径测试计划

目标：在 **真实 WebApi 库** 写入可辨识主数据与单据，**测完不删除**，便于在 Vue（`http://localhost:5173`）按前缀检索验收。

配套脚本：[`scripts/run-retain-cases.ps1`](./scripts/run-retain-cases.ps1)  
报告：[`reports/retain/`](./reports/retain/)  
与只读冒烟的区别见 [`TEST-REPORT-2026-09-13.md`](./TEST-REPORT-2026-09-13.md)。

---

## 1. 数据约定（强制）

| 项 | 规则 |
|----|------|
| 仓库 | `RETAIN_WH`（名称：Retain Test WH） |
| 库位 | `RETAIN.RECV-01`（收货/平库同址）、`RETAIN.STK-A`（保留位，出库后仍可查流水） |
| 物料 | `RETAIN-MAT-01` |
| 容器/托盘 | `RETAIN-TP-{stamp}-*` |
| 单据号 | `RETAIN-IN-*` / `RETAIN-OUT-*` / `RETAIN-CC-*` |
| 申请点 | `RETAIN.RP-01`（`StkRequestPoint`） |
| stamp | `yyyyMMdd-HHmmss`（单次运行唯一） |
| 清理 | **默认不清理**；若需清库另跑 `scripts/cleanup-retain-prefix.ps1`（可选，本次不自动执行） |

---

## 2. 场景清单

| CaseId | 步骤 | 期望落库 | Automation |
|--------|------|----------|------------|
| TC-RETAIN-001 | 幂等确保仓/位主数据 | `Wms_Warehouse` / `Wms_Location` | ApiHttp |
| TC-RETAIN-002 | 入库建单→审核→同址 receive | 入库单 Completed + `Wms_Stock` | ApiHttp |
| TC-RETAIN-003 | 再组一盘库存（同址） | 第二托盘库存保留 | ApiHttp |
| TC-RETAIN-004 | 出库：审核→Ship 扣减部分 | 出库单 Completed；库存减少但不全空 | ApiHttp |
| TC-RETAIN-005 | 盘点：建计划→录数→确认 | 盘点单 + 库存调到录数 | ApiHttp |
| TC-RETAIN-006 | 种子申请点 + Ops 启停往返 | `Stk_RequestPoint` 保留；enable 恢复 true | ApiHttp |
| TC-RETAIN-007 | 菜单仍无执行运维（回归） | 不改菜单 | ApiHttp |
| TC-RETAIN-008 | 浏览器打开库存/入库页可见 RETAIN | UI 可见 | Browser |

未强依赖地图的四向运维入库（需 Gateway）标为 **可选**：失败记 SKIP，不阻断。

---

## 3. 验收（人眼）

登录后在侧栏：

1. **仓储WMS → 仓库** 搜 `RETAIN_WH`  
2. **库位** 搜 `RETAIN.`  
3. **入库单** 搜 `RETAIN-IN-`  
4. **出库单** 搜 `RETAIN-OUT-`  
5. **库存** 搜 `RETAIN-MAT` / `RETAIN-TP`  
6. **立库 → 申请点** 搜 `RETAIN.RP`

---

## 4. 命令

```powershell
powershell -ExecutionPolicy Bypass -File Seven.Master\testplan\scripts\run-retain-cases.ps1
cd Seven.Master\testplan\e2e
npx playwright test specs/retain-ui.spec.ts
```
