# 跨包闭环与运维 IA 测试计划

设计：[`design/ops/01`](../design/ops/01-ops-menu-restructure.md)、[`design/wms/02`](../design/wms/02-location-multi-pack-prefix.md)  
详细用例：[`cases/cross-pack-cases.md`](./cases/cross-pack-cases.md)

## 范围

一仓多包前缀、CanHandle、HandoverLink、Trigger 静默矩阵、取消执行运维后的菜单、联锁写权威、监控 Pack 过滤、黄金冒烟剧本 A–E。

## Features

DUAL（stacker+fourway）为主；菜单用例覆盖 STK-only / FW-only。

## 覆盖与缺口

| 主题 | 现状 |
|------|------|
| 前缀/多包 | ✅ PackPrefixAndMultiPackTests |
| 交接总线 | ✅ OrchestrationBusTests |
| 静默 | ✅ 部分 DualPack |
| 菜单 IA / 钉底 | ❌ Browser（运维已落地，需 UI 回归） |
| 联锁双边 | 部分 Platform；UI 缺口 |

## 回归门禁

- [ ] cases 全部 P0  
- [ ] 剧本 A–E 至少 Manual/DotNet 各跑通一次  
- [ ] 无「执行运维」菜单（种子退役后）
