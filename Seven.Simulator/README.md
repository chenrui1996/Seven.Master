# Seven.Simulator

立库/四向联调模拟器：编辑地图 → Deploy 主数据 → 仿真（Trigger / Gateway）→ Promote 生产。

| 文档 | 说明 |
|------|------|
| [doc/21-仿真器与联调闭环](../doc/21-仿真器与联调闭环.md) | **实施流程**（操作 + 开发四期） |
| [定稿规格](../docs/superpowers/specs/2026-08-29-seven-simulator-design.md) | 架构、工程模型、API |
| [开发计划](../docs/superpowers/plans/2026-08-29-seven-simulator.md) | Task 级执行清单 |
| [设计摘要](../design/2026-08-29-seven-simulator-design.md) | 一页纸决策 |

技术栈与 `Seven.Vue3` 对齐：Vue 3 · Vite · Pinia · Element Plus · axios · SignalR（三期+ three）。

决策：从 RCS `simulation-spa` **能力全量 Vue 重写**（不嵌原生 SPA、不引 LES 程序集）；一期先打通闭环，二～四期增强编辑器 / Gateway / 导入。

## 开发

```powershell
# WebApi
cd ..\Seven.Net8
dotnet run --project Seven.WebApi

# 模拟器（另开终端）
cd ..\Seven.Simulator
npm install
npm run dev
```

默认 http://localhost:5174 ，`/api` 代理到 WebApi（见 `vite.config.ts`）。

## 认证（Trigger / 入库单）

`/api/Wcs/Triggers/*` 与 `/api/WmsInboundOrder/*` 需 JWT。与 **Seven.Vue3** 共用 `localStorage` 键 `token`：

1. 浏览器打开 Vue3（通常 http://localhost:5173）并登录。
2. 同一浏览器打开 Simulator；`http.ts` 会自动附带 `Authorization: Bearer …`。
3. 若 Player 提示 401，请重新登录 Vue3 或检查 WebApi CORS/代理。

Simulation Deploy / Reset / Promote 接口为 `[AllowAnonymous]`，无需 token。

## 页面

| 路由 | 阶段 |
|------|------|
| `/features` | 选 Features，导出 appsettings / 校验 |
| `/map` | 编辑/导入地图，Deploy / Undeploy |
| `/player` | 仿真（Trigger；后期 Gateway + 3D） |
| `/promote` | 真机地址与 Promote |

## API（与 WebApi 对齐）

路由来源：`SimulationController.cs`（`api/simulation`，`[AllowAnonymous]`）。完整说明见 [doc/21](../doc/21-仿真器与联调闭环.md) §5。

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/simulation/projects/validate-features` | Features 校验 |
| POST | `/api/simulation/deploy` | 写 `Wms_*` + 包种子 + `Sim_Deployment` |
| POST | `/api/simulation/undeploy` | 拆除部署 |
| GET | `/api/simulation/deployments` | 部署记录 |
| POST | `/api/simulation/reset` | 清运行态，保留 `SIM_` 主数据 |
| POST | `/api/simulation/promote-preview` | Promote 预览（拒环回地址） |
| POST | `/api/simulation/promote` | 写入 CommConnection + 标记 Promoted |
| POST | `/api/Wcs/Triggers/*` | 仿真档 A（需 JWT） |
| POST | `/api/WmsInboundOrder/add` | 快捷入库建单（需 JWT） |
| POST | `/api/WmsInboundOrder/approve/{id}` | 快捷入库审核（需 JWT） |

**Phase I 请求体：** `reset` → `{ projectName }`；`undeploy` → `{ projectName, removeLocations? }`；`promote-preview` / `promote` → `{ projectName, devices: [{ code, host, port, protocol }] }`（环回 Host 被 API 拒绝）。

`Features.Simulator` 仅影响入口；Deploy API 始终可用。

## 手动验收清单（一期）

- [ ] Features 导出片段与工程 meta 一致
- [ ] MapEditor：节点、边、申请点可编辑；Deploy / Undeploy 成功
- [ ] Player：Trigger 可发；Reset 清运行态不删库位
- [ ] Player：快捷入库建单+审核（需 Vue3 登录 token）
- [ ] Promote：预览/执行；UI 与 API 均拒绝 127.0.0.1 / localhost / ::1
- [ ] 导入旧版 `.sevenproj.json` 自动补齐 scada / promote / simCommsMode
