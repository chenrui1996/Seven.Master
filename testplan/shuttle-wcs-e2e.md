# 四向穿梭车 WCS 测试计划

设计：[`design/shuttle-wcs/04`](../design/shuttle-wcs/04-end-to-end-flow.md)、[`05`](../design/shuttle-wcs/05-ops-menu-and-process.md)  
调度逻辑：[`doc/keypoint/10-四向车调度完整逻辑`](../doc/keypoint/10-四向车调度完整逻辑.md)  
详细用例：[`cases/shuttle-wcs-cases.md`](./cases/shuttle-wcs-cases.md)  
新加坡地图场景：[`cases/fourway-scheduling-singapore.md`](./cases/fourway-scheduling-singapore.md)  
Fixture：[`fixtures/singapore-fourway-map.json`](./fixtures/singapore-fourway-map.json)

## 范围

层巷位分配、同层入出、停车账本、跨层提升、交通占边、运维五页（监控/入库/穿梭/提升/联锁）与 Ops API。  
地图基线：`20260619-singaporeshuttleproj.simproj.json`（双层 + PalletLift）。

## Features

`wms` + `orchestrationBus` + `wcsPacks.fourWay`

## 覆盖与缺口

| 主题 | 现状 |
|------|------|
| 入出库/分配/路径/停车/提升 | ✅ FourWay* E2E |
| Ops inbound/point/charge/force/resend | ❌ 待 FourWayOps*Tests |
| 运维 UI / 互斥展示 | ❌ Browser |

## 回归

```powershell
dotnet test --filter "FullyQualifiedName~FourWay|FullyQualifiedName~InboundToFourWay|FullyQualifiedName~OutboundToFourWay"
# 预留
dotnet test --filter "FullyQualifiedName~FourWayOps"
```
