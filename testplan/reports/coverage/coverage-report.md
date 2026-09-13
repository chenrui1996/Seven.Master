# 全表覆盖测试报告（终态）

- **日期**：2026-09-13
- **脚本**：`scripts/run-full-coverage-cases.ps1` + Ops 强制完成 + 演示任务 SQL 补齐
- **计划/矩阵**：[`cases/table-coverage-matrix.md`](../cases/table-coverage-matrix.md)
- **操作指南**：[`../../doc/25-联调示例数据操作指南.md`](../../doc/25-联调示例数据操作指南.md)

## 1. Case 结果（主脚本轮）

| CaseId | Status | 说明 |
|--------|--------|------|
| TC-COV-000 | PASS | 登录 |
| TC-COV-MD-001 | PASS | Zone/Layer/Aisle/ContainerType/Locations |
| TC-COV-MD-002 | PASS | HandoverLink |
| TC-COV-STK-MD | PASS | 立库策略/路网/Profile/Coder |
| TC-COV-FW-MD | PASS | 四向地图/策略/停车/提升机 |
| TC-COV-STK-IN | PARTIAL→补救 | 首轮 `no pack or handover`；后续 Bus/任务由 FW 交接链与强制完成产生 |
| TC-COV-FW-IN | PASS | 异址组盘；后经 Ops force-complete 落 FwPutAway/Shuttle |
| TC-COV-PICK / STK-OUT | PASS | generatePicks + 出库 |
| TC-COV-XFER | PASS | 调拨完成 |
| TC-COV-CTL / SCD / IFC | PASS | 联锁 / SCADA / 接口日志可读 |
| TC-COV-OPT-DC | SKIP | DeviceComm Feature 关 |

## 2. 表探针终态（40/40 非空）

见 [`table-snapshot-final.json`](./table-snapshot-final.json)。

原先为 0、现已有样例行的关键任务表：

| API | Total |
|-----|------:|
| StkPutAwayTask | 1 |
| StkRetrievalTask | 1 |
| StkDeviceTask | 1 |
| FwPutAwayTask | 1 |
| FwShuttleTask | 1 |
| FwRetrievalTask | 1 |
| FwHoistTask | 1 |
| FwHoistExecTask | 1 |
| BusTransportOrder | 4 |
| BusTransportLeg | 2 |

主数据、单据、库存、调拨、停车、地图等均 ≥1。

## 3. 用例缺口结论

| 结论 | 说明 |
|------|------|
| 场景用例（testplan/cases） | WMS/SRM/FW/跨包/Ops **逻辑面**已有；缺的是**活库行覆盖** |
| 本次补全 | `TC-COV-*` 矩阵 + 全覆盖脚本 + doc/25 示例 |
| 仍弱 | 跨层 Hoist **真闭环**（现为演示行）；DeviceComm/Outbox（Features 关）；`Stk_RouteFlow`/`AssignmentRecord` 无列表 API 时需 SQL 验收 |

## 4. UI 查找关键词

`RETAIN_WH` · `COV.` · `Stk.RECV-01` · `Fw.L01` · `COV-IN-` · `COV-XFER-` · `RP_IN_01` · `COV-VIEW-01`
