# 测试执行总报告

- **执行日**：2026-09-13
- **环境**：WebApi `http://localhost:5000` · Vue `http://localhost:5173` · 账号 `admin` / `123456`（验证码开启）
- **Features（实测）**：`wms=true` · `orchestrationBus=true` · `wcsPacks.stacker/fourWay/boxSort=true` · `captcha=true` · `simulator=true`

---

## 1. 结果摘要

| 层 | 工具 | 通过 | 失败 | 跳过/阻塞 | 产物 |
|----|------|------|------|-----------|------|
| DotNet | `dotnet test` → 独立 OutputPath | **187** | **0** | 0 | [reports/dotnet/](./reports/dotnet/) |
| ApiHttp | `scripts/run-api-cases.ps1` | **29** | **0** | **1** SKIP | [reports/api/api-report.md](./reports/api/api-report.md) |
| Browser | Playwright `@playwright/test` | **8** | **0** | Chrome MCP 不可用（已用 Playwright 替代） | [reports/browser/](./reports/browser/) |
| Manual / 未跑闭环 | — | — | — | 见 [failed/](./failed/) | 运维入库互斥、真实 force-complete 有任务等 |

**结论**：服务级回归与运维 IA/Ops 读路径、登录与关键页面冒烟 **全部通过**。需现场任务数据的写路径与部分 P1 剧本列入未通过/未执行清单，待补数据后复测。

---

## 2. DotNet（ExistingTest）

命令（避开正在运行的 WebApi 文件锁）：

```powershell
cd Seven.Net8
dotnet test Seven.Tests\Seven.Tests.csproj `
  -o ..\testplan\reports\testbin `
  --logger "trx;LogFileName=dotnet-all.trx" `
  --results-directory ..\testplan\reports\dotnet
```

覆盖类（节选）：`WmsOrderServiceTests`、`InboundTo*E2E`、`OutboundTo*E2E`、`FourWay*`、`Stacker*`、`OrchestrationBusTests`、`PackPrefixAndMultiPackTests`、`PlatformIfcCtlTests`、`AuthApiTests`、`PdaServiceTests` 等。

**缺口**：尚无 `FourWayOpsServiceTests` / `StackerOpsServiceTests`（用例标 FutureTest）；本次用 **ApiHttp** 补了 Ops 读路径与负例。

---

## 3. ApiHttp（活库）

脚本：[scripts/run-api-cases.ps1](./scripts/run-api-cases.ps1)

已覆盖关键 CaseId：`TC-X-012/013/020/021/040/041`、`TC-OPS-FW-001～003/006/010/013/020`、`TC-OPS-STK-001～003/006～008`、`TC-WMS-041`、认证与 WMS 分页冒烟。

首轮曾失败（契约澄清后已绿）：

| CaseId | 根因 | 修正 |
|--------|------|------|
| TC-X-021 | `control-mode` 的 `mode` 须为 **数值枚举**（0=Auto,1=Manual），字符串 `Manual` → 400 | 脚本改传 `mode:1` |
| TC-OPS-STK-003 | Body 字段为 `targetType`，非 `kind` | 改 `targetType=device` |

详见 [failed/history-first-run.md](./failed/history-first-run.md)。

---

## 4. Browser

- Chrome MCP（superpowers-chrome）启动即 `Connection closed`，无法附着用户已开 Chrome。
- 已在 [`testplan/e2e`](./e2e/) 安装 Playwright Chromium，规格 [`e2e/specs/ops-ui.spec.ts`](./e2e/specs/ops-ui.spec.ts)。

```powershell
cd Seven.Master\testplan\e2e
npx playwright test
```

**8/8 PASS**：登录+验证码、无「执行运维」、四向/立库 Ops Monitor、接口日志、入库快捷、运维入库页、联锁页。

---

## 5. 数据与菜单实测要点

- `getMenu`：**无**「执行运维」；`StkOpsFolder` / `FwOpsFolder` 分别挂在立库/四向包下。
- `getMenuList`：遗留「执行运维」`enable=0`（及子项联锁/2D）。
- `IfcApiLog` 父节点 = **系统管理**。
- 申请点运维列表当前 **空** → `TC-OPS-STK-006b` SKIP。

---

## 6. 复跑清单

1. DotNet（独立 `-o`）  
2. `powershell -File testplan\scripts\run-api-cases.ps1`  
3. `cd testplan\e2e; npx playwright test`  
4. 有任务数据后：Ops inbound / force-complete 真值 / 出入互斥（见 failed）
