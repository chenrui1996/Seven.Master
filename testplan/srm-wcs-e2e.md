# 立库（堆垛）WCS 测试计划

设计：[`design/srm-wcs/03`](../design/srm-wcs/03-end-to-end-and-ops.md)、[`doc/20`](../doc/20-WMS与WCS实现说明.md) 第二部分  
调度逻辑：[`doc/keypoint/11-立库调度完整逻辑`](../doc/keypoint/11-立库调度完整逻辑.md)  
详细用例：[`cases/srm-wcs-cases.md`](./cases/srm-wcs-cases.md)  
SRM Demo 场景：[`cases/stacker-scheduling-srm-demo.md`](./cases/stacker-scheduling-srm-demo.md)  
Fixture：[`fixtures/srm-demo-stacker-map.json`](./fixtures/srm-demo-stacker-map.json)

## 范围

分配（巷/位/双深）、寻路拆腿、段反馈、Pri、深浅 Transfer、Blocking、双包静默、**运维四页与 Ops API**（已落地）。  
地图基线：`20260426-SRM-Demo.simproj.json`（4 巷双深 · 320 货位 · 2 SRM）。

## Features

`wms` + `orchestrationBus` + `wcsPacks.stacker`

## 覆盖与缺口

| 主题 | 自动化现状 |
|------|------------|
| 分配/双深/路径 | ✅ Stacker* / DoubleDeep / Path / Router |
| 入出库 E2E | ✅ InboundToStacker / OutboundToStacker |
| Ops force/resend/board | ❌ 待 `StackerOpsServiceTests` / ApiHttp |
| 运维菜单 UI | ❌ Browser |

## 回归

```powershell
dotnet test --filter "FullyQualifiedName~Stacker|FullyQualifiedName~InboundToStacker|FullyQualifiedName~OutboundToStacker|FullyQualifiedName~StackerDoubleDeep|FullyQualifiedName~StackerPath|FullyQualifiedName~StackerRouter"
# 预留
dotnet test --filter "FullyQualifiedName~StackerOps"
```

- [ ] cases 全部 P0  
- [ ] 与 WMS-002/004/011 联调  
- [ ] Ops P0（ApiHttp 或 Manual 至脚本落地）
