# WMS 端到端测试计划

设计：[`design/wms/`](../design/wms/)、[`doc/20`](../doc/20-WMS与WCS实现说明.md) 第一部分  
详细用例：[`cases/wms-cases.md`](./cases/wms-cases.md)  
约定：[`00-conventions.md`](./00-conventions.md)

## 范围

- 入库：同址完成 / 异址建运 / 完成回写 / 拒批  
- 出库：审核 Book、建运、完成扣账、平库直发、运输中禁 Ship  
- 盘点：建计划、录数、调账、未齐盘禁确认  
- PDA：收货/上架/盘点冒烟  
- 接口日志菜单位置（系统管理）  
- 运输完成幂等

## 环境

| 项 | 要求 |
|----|------|
| Features | 至少 `wms`；运输场景加 `orchestrationBus` + 包 |
| DB | 测试 InMemory 或干净演示库 |
| 设备 | Trigger 仿真即可 |

## 覆盖与缺口

| 主题 | 计划 ID | 自动化现状 |
|------|---------|------------|
| 同址入库 | WMS-01 | DotNet 部分有 |
| 异址建运 | WMS-02 | `InboundToStacker/FourWayE2E` |
| 回写落账 | WMS-03 | 同上 |
| 出库 Book/建运 | WMS-04 | `OutboundTo*E2E` |
| 出库完成 | WMS-05 | 同上 |
| 平库直发 | WMS-06 | `WmsOrderServiceTests` |
| 盘点 | WMS-07 | `CycleCount_*` / PDA |
| PDA | WMS-08 | `PdaServiceTests` |
| Ifc 菜单 | WMS-09 | Manual/Browser；日志写 `PlatformIfcCtlTests` |
| HTTP 全链路 | — | **缺口** → FutureTest `WmsHttpE2ETests` |

## 回归清单

- [ ] [`cases/wms-cases.md`](./cases/wms-cases.md) 全部 P0  
- [ ] Dual 下 PackId 解析不错包  
- [ ] Complete 幂等不双记账  
- [ ] `dotnet test --filter "FullyQualifiedName~Wms|FullyQualifiedName~Pda|FullyQualifiedName~Stock|FullyQualifiedName~InboundTo|FullyQualifiedName~OutboundTo"`
