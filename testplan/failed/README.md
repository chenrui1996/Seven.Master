# 未通过 / 未执行 / 阻塞用例

本目录只收：**FAIL**、**SKIP**、**BLOCKED**、以及本次未能完成闭环的 **P0/P1**（相对 [cases/](../cases/)）。

执行总报告：[../TEST-REPORT-2026-09-13.md](../TEST-REPORT-2026-09-13.md)

---

## 1. 本次 FAIL

无（DotNet 0 · ApiHttp 0 · Playwright 0）。

---

## 2. SKIP

| CaseId | 原因 | 后续 |
|--------|------|------|
| TC-OPS-STK-006b | 活库 `GET .../request-points` 返回空列表，无法做停用/启用往返 | 种子或手工插入 `StkRequestPoint` 后复跑 ApiHttp |

---

## 3. BLOCKED（基础设施）

| 项 | 原因 | 替代 |
|----|------|------|
| Chrome MCP 附着用户 Chrome | `use_browser` navigate/list_tabs → Connection closed | 已用 Playwright 独立 Chromium 完成 UI 冒烟 |

---

## 4. 未执行（缺业务任务数据 / 风险写路径）

下列用例已有 DotNet 或文档，但 **活库 ApiHttp/Browser 未做完整写闭环**（避免污染生产态库存）；需准备隔离仓数据后补跑：

| CaseId | 说明 | 建议 Automation |
|--------|------|-----------------|
| TC-OPS-FW-004 | 运维轻量入库 | ApiHttp + 仿真 Trigger |
| TC-OPS-FW-005 | 出入互斥 | ApiHttp |
| TC-OPS-FW-007～009 | 指定点/充电 | ApiHttp（WCS Free） |
| TC-OPS-FW-011～012 | Hoist 强制完成 / 重发门控 | ApiHttp + 预置任务 |
| TC-OPS-STK-004～005 | PutAway 强制完成 / 重发 | ApiHttp |
| TC-X-030～034 | 黄金剧本 A–E 活库 | DotNet 已有部分；活库需种子托盘 |
| TC-WMS-001～023 活库 HTTP | 建单审核组盘 | ApiHttp（建议专用 WH） |
| TC-OPS-FW-021/023 | 穿梭四段布局 / 轮询 | Browser 细化断言 |

---

## 5. 新发现缺口（已回写 cases）

见 [../cases/discovered-2026-09-13.md](../cases/discovered-2026-09-13.md)：

- `control-mode.mode` 数值枚举契约  
- `force-complete.targetType` 字段名  
- Captcha API 返回明文 code（联调便利 / 安全注意）  
- 活库无申请点时的 SKIP 策略  

---

## 6. 首轮失败留痕

见 [history-first-run.md](./history-first-run.md)（契约修正前）。
