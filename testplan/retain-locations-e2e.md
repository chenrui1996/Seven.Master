# 库位落库并保留 — 测试计划

目标：把 **新加坡四向 / SRM Demo 立库** fixture 中的库位与层级主数据写入 **活库 MySQL（seven）**，**测完不删除**，并在报告中给出 UI 检索前缀。

配套脚本：[`scripts/run-retain-locations.ps1`](./scripts/run-retain-locations.ps1)  
地图源：[`fixtures/singapore-fourway-map.json`](./fixtures/singapore-fourway-map.json)、[`fixtures/srm-demo-stacker-map.json`](./fixtures/srm-demo-stacker-map.json)  
调度逻辑：[keypoint/10](../doc/keypoint/10-四向车调度完整逻辑.md)、[keypoint/11](../doc/keypoint/11-立库调度完整逻辑.md)

与既有脚本差异：

| 脚本 | 库位规模 | 说明 |
|------|----------|------|
| `run-retain-cases.ps1` | 2 个 `Stk.RETAIN.*` | 单据/库存冒烟 |
| `run-full-coverage-cases.ps1` | ~5 个 Stk/Fw | 表覆盖造数 |
| **本计划** | **SRM Demo 320 + 新加坡 Fw 货位节点 + 层级** | 地图级库位落库保留 |

---

## 1. 数据约定（强制保留）

| 项 | 编码 |
|----|------|
| 仓库 | `RETAIN_WH`（复用） |
| 库区 | `MAP.Z-STK` / `MAP.Z-FW` |
| 立库巷道 | `Stk.A1`…`Stk.A4`（对应 Demo 四架） |
| 立库货位 | `Stk.{coordPointId}`（如 `Stk.l201011`） |
| 四向层 | `Fw.L01` / `Fw.L02` |
| 四向巷 | `Fw.A1` / `Fw.A2` |
| 四向货位 | fixture 中 `kind=Location` 的节点码（`Fw.*.S*`） |
| 收货位 | `Stk.RECV-MAP` / `Fw.RECV-MAP`（本跑次确保存在） |
| 清理 | **默认不清理** |

参数：

```powershell
# 全量（默认）
powershell -File testplan\scripts\run-retain-locations.ps1

# 仅立库 / 仅四向 / 限制数量（冒烟）
powershell -File testplan\scripts\run-retain-locations.ps1 -Pack stacker -MaxLocations 40
powershell -File testplan\scripts\run-retain-locations.ps1 -Pack fourway
```

---

## 2. 用例清单

| CaseId | 步骤 | 期望落库 | Automation |
|--------|------|----------|------------|
| TC-LOC-000 | 登录 | Token | ApiHttp |
| TC-LOC-001 | 确保仓 `RETAIN_WH` | `Wms_Warehouse` | ApiHttp |
| TC-LOC-010 | 立库 Zone/Aisle ×4 | `Wms_Zone` / `Wms_Aisle` | ApiHttp |
| TC-LOC-011 | 灌入 SRM Demo 货位（幂等） | `Wms_Location` PackId=stacker | ApiHttp |
| TC-LOC-012 | 抽样校验 depth1/depth2 成对存在 | 各 ≥1 | ApiHttp |
| TC-LOC-020 | 四向 Zone/Layer/Aisle | `Wms_Layer` 等 | ApiHttp |
| TC-LOC-021 | 灌入新加坡 Location 节点 | `Wms_Location` PackId=fourway | ApiHttp |
| TC-LOC-030 | 全表探针：`WmsLocation.total` ≥ 基线 | 报告记数 | ApiHttp |
| TC-LOC-031 | 在收货位落 1 托库存（可选可见性） | `Wms_Stock` KEEP | ApiHttp |
| TC-LOC-040 | UI 检索清单写入报告 | — | Manual/报告 |

---

## 3. 验收（人眼）

登录 `http://localhost:5173`：

1. **仓储WMS → 库位** 搜 `Stk.l` 或 `Stk.A`（Demo 货位）  
2. 搜 `Fw.`（四向货位 / 收货）  
3. **巷道** 搜 `Stk.A` / `Fw.A`  
4. **层** 搜 `Fw.L`  
5. **库存** 搜 `MAP-LOC-TP`（若 TC-LOC-031 PASS）

---

## 4. 报告

`testplan/reports/retain-locations/retain-locations-report-*.md`  
产物 JSON：同目录 `*-artifacts.json`（含创建/已存在计数、抽样码）。
