# 全业务表覆盖矩阵与补全用例

对照活库探针（`reports/retain/table-coverage-probe.json`）与 `SevenDbContext` 业务表族。  
**目标**：除可选子系统（DeviceComm/Outbox/Alarm/Sim 关 Features）外，核心 WMS/Bus/Stk/Fw/Platform 表均有至少一行**可辨识示例数据**（前缀 `COV` / `RETAIN` / `Stk.` / `Fw.`）。

执行脚本：[`scripts/run-full-coverage-cases.ps1`](./scripts/run-full-coverage-cases.ps1)  
报告：[`reports/coverage/`](./reports/coverage/)  
操作指南示例：[`../doc/25-联调示例数据操作指南.md`](../doc/25-联调示例数据操作指南.md)

---

## 1. 覆盖状态（执行前探针 → 目标）

| 表族 | 探针多为 0 的表 | 补全 CaseId | 手段 |
|------|-----------------|-------------|------|
| WMS 主数据 | Zone/Layer/Aisle/ContainerType | TC-COV-MD-001 | CRUD seed |
| WMS 交接 | HandoverLink | TC-COV-MD-002 | CRUD seed |
| Bus | TransportOrder/Leg | TC-COV-STK-IN | 异址组盘+仿真 Trigger |
| Stk 策略/路网/Profile/Coder | Policy/Route/Profile/Coder | TC-COV-STK-MD | CRUD seed |
| Stk 任务 | PutAway/Retrieval/DeviceTask | TC-COV-STK-IN / OUT | 入出库闭环 |
| Fw 地图/策略/RP | Map/Node/Route/Policy/RP | TC-COV-FW-MD | CRUD seed |
| Fw 停车/提升机 | Parking/Hoist* | TC-COV-FW-MD | CRUD seed |
| Fw 任务 | PutAway/Shuttle/Retrieval/Hoist* | TC-COV-FW-IN | 四向异址组盘+Trigger |
| Ctl | Ctl_Mode | TC-COV-CTL | Ops control-mode |
| Ifc | Ifc_ApiLog | TC-COV-IFC | 读 InterfaceLog；可选非法请求 |
| Biz | TransferOrder | TC-COV-XFER | 调拨 create/approve/complete |
| Scada | Scd_View | TC-COV-SCD | CRUD add 视图 |
| Picking | 已有 1 行 | TC-COV-PICK | generatePicks |
| Dc/Mq/Alarm/Sim | Features 关 | TC-COV-OPT-* | SKIP 或轻量 seed |

---

## 2. 新增/强调用例

### TC-COV-MD-001 WMS 主数据 Zone/Layer/Aisle/ContainerType

| 字段 | 内容 |
|------|------|
| Automation | ApiHttp |
| Hook | `hook:wms.master.seed` |
| 期望 | `COV.Z01` / `Fw.L01` / `Stk.A1`/`Fw.A1` / `COV-PALLET` 可查 |

### TC-COV-MD-002 HandoverLink

| Automation | ApiHttp |
| Hook | `hook:wms.handover.seed` |
| 期望 | `Stk.HO-01` 交接位 + `Wms_HandoverLink` FromPack=stacker ToPack=fourway |

### TC-COV-STK-MD 立库策略/路网/双深/点码

种子 `Stk_AssignmentPolicy`、`Stk_Route`、`Stk_LocationProfile`、`Stk_DeviceCoder`、额外 `Stk_RequestPoint`。

### TC-COV-STK-IN 立库异址入库闭环（写 Bus+PutAway+Device）

`buildPallet` 收货≠目标 → Trigger destination-request + segment-feedback → 库存到 `Stk.LOC-A1-01`。

### TC-COV-STK-OUT 立库出库建运（写 Retrieval）

对目标位库存建出库审核（有 Bus 时）或平库 Ship；优先触发 Retrieval。

### TC-COV-FW-MD / TC-COV-FW-IN

四向主数据 + 异址入库仿真，期望 `Fw_*` 任务/地图非空。

### TC-COV-XFER 仓内调拨

`TransferOrder` add → approve → complete，搬移 `COV` 库存。

### TC-COV-CTL / TC-COV-SCD / TC-COV-IFC / TC-COV-PICK

联锁写回、SCADA 视图、接口日志页、出库 generatePicks。

---

## 3. 与既有 TC 映射

| 既有 | 本矩阵 |
|------|--------|
| TC-WMS-* 同址 | 已由 RETAIN 覆盖 |
| TC-SRM / TC-FW DotNet | InMemory 已覆盖逻辑；本矩阵补**活库行** |
| TC-X-030～034 | 活库黄金剧本部分由此脚本落地 |
| TC-OPS-* | 读路径已测；本矩阵补主数据使 Ops board 有内容 |

---

## 4. 命令

```powershell
powershell -ExecutionPolicy Bypass -File Seven.Master\testplan\scripts\run-full-coverage-cases.ps1
```
