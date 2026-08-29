# Seven.Simulator

立库联调模拟器：Features → 地图 → 仿真 → 生产。设计见 [`../design/2026-08-29-seven-simulator-design.md`](../design/2026-08-29-seven-simulator-design.md)。

技术栈与 `Seven.Vue3` 对齐：Vue 3 · Vite · Pinia · Element Plus · axios · SignalR。

## 开发

```powershell
# WebApi：Development 默认 Features.Simulator/Wms/OrchestrationBus/Stacker = true
cd ..\Seven.Net8
dotnet ef database update --project Seven.Infrastructure --startup-project Seven.WebApi --context SevenDbContext
dotnet run --project Seven.WebApi

# 另开终端
cd ..\Seven.Simulator
npm install
npm run dev
```

默认 http://localhost:5174 ，`/api` 代理到 `http://localhost:5000`（按实际 WebApi 端口调整 `vite.config.ts`）。

## API（需 `Features.Simulator=true`）

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/simulation/projects/validate-features` | 校验工程 Features |
| POST | `/api/simulation/deploy` | 写入 `Wms_*` + 包种子 + `Sim_Deployment` |
| POST | `/api/simulation/undeploy` | 标记 Undeployed（可选删空库位） |
| GET | `/api/simulation/deployments` | 最近部署记录 |

## 页面

| 路由 | 阶段 |
|------|------|
| `/features` | 选 Features，导出 appsettings / 服务端校验 |
| `/map` | 导入或绘制节点，调用 Deploy/Undeploy |
| `/player` | 单机 Trigger 仿真（对接现有 API） |
| `/promote` | 真机地址与 Promote 占位 |

## 说明

- Deploy 已接通；Promote 仍为后续 Phase。
- 不引用 RCS4Shuttle 程序集；流程概念参考其 simulation-spa。
