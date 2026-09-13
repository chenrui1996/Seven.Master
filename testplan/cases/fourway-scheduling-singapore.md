# 四向调度场景用例（新加坡地图）

约定：[`../00-conventions.md`](../00-conventions.md)  
逻辑：[`../../doc/keypoint/10-四向车调度完整逻辑.md`](../../doc/keypoint/10-四向车调度完整逻辑.md)（§9 差距矩阵）  
地图 fixture：[`../fixtures/singapore-fourway-map.json`](../fixtures/singapore-fourway-map.json)  
源 simproj：`20260619-singaporeshuttleproj.simproj.json`

**种子约定**：DotNet 用例以 fixture 灌入 `Fw_MapVersion/Node/Route`（见 `FourWaySingaporeMapTests`）；点码与 fixture 一致。

**差距标签**：`Gap:G-FW-xx` 对照文档 §9.2；未实现能力标 `Type=Gate`。

---

## A. 路径与交通

### TC-FW-SG-001 层内 Dijkstra 最短路（L01）

| 字段 | 内容 |
|------|------|
| ID | TC-FW-SG-001 |
| Priority | P0 |
| Features | FW |
| Automation | DotNet |
| Fixture | singapore L01 nodes/routes |
| ExistingTest | `FourWaySingaporeMapTests.Dijkstra_L01_MainTrack_ShouldFindPathAlongRW1` |

**前置** 灌入 L01 图。  
**步骤** `FourWayRouter.FindPath(RW1_1Z001020601 → RW1_1Z001060601)`。  
**期望** 路径含中间主轨点；首末点正确。

---

### TC-FW-SG-002 占边互斥（冲突路径）

| 字段 | 内容 |
|------|------|
| ID | TC-FW-SG-002 |
| Priority | P0 |
| Type | HappyPath / Negative |
| Automation | DotNet |
| Gap | G-FW-02（不绕路，仅等待） |
| ExistingTest | `FourWaySingaporeMapTests.Traffic_SameEdge_SecondStaysRouting_UntilRelease` |

**期望** 先到 Running；后到 **Routing** 不 Failed；Release + Retry 后 Running。

---

### TC-FW-SG-003 按 LayerCode 选图（L01 vs L02）

| 字段 | 内容 |
|------|------|
| ID | TC-FW-SG-003 |
| Priority | P0 |
| Automation | DotNet |
| ExistingTest | `FourWaySingaporeMapTests.ResolveMap_ShouldUseLayerCode_NotCrossLayerRoutes` |

**期望** L01 任务 Path 的 Edge 全部属于 L01 MapVersion。

---

## B. 入库分配与任务拆分

### TC-FW-SG-010 层→巷→位

| 字段 | 内容 |
|------|------|
| ID | TC-FW-SG-010 |
| Priority | P0 |
| Automation | DotNet / ApiHttp |
| ExistingTest | `InboundToFourWayE2ETests`；`FourWayAllocatorTests` |

---

### TC-FW-SG-011 MaxShuttleCount 跳巷

| 字段 | 内容 |
|------|------|
| ID | TC-FW-SG-011 |
| Priority | P1 |
| Type | Negative |
| Automation | DotNet |
| ExistingTest | `FourWayParkingTests.SelectAisle_ShouldSkip_WhenMaxShuttleReached` |

---

### TC-FW-SG-012 同层入库闭环

| 字段 | 内容 |
|------|------|
| ID | TC-FW-SG-012 |
| Priority | P0 |
| Automation | DotNet |
| ExistingTest | `InboundToFourWayE2ETests` |

---

## C. 停车与出库

### TC-FW-SG-020 停车 Free→Reserved→Occupied

| 字段 | 内容 |
|------|------|
| ID | TC-FW-SG-020 |
| Priority | P0 |
| Gap | G-FW-04（非就近） |
| ExistingTest | `FourWayParkingTests` / `OutboundToFourWayE2ETests` |

---

### TC-FW-SG-021 无车位 Suspended 再唤醒

| 字段 | 内容 |
|------|------|
| ID | TC-FW-SG-021 |
| Priority | P0 |
| ExistingTest | `OutboundToFourWayE2ETests` |

---

### TC-FW-SG-022 同组 Pri 门闩

| 字段 | 内容 |
|------|------|
| ID | TC-FW-SG-022 |
| Priority | P0 |
| ExistingTest | `OutboundToFourWayE2ETests` |

---

## D. 提升机

### TC-FW-SG-030 跨层三阶段

| 字段 | 内容 |
|------|------|
| ID | TC-FW-SG-030 |
| Priority | P0 |
| ExistingTest | `FourWayHoistE2ETests` |

---

### TC-FW-SG-031 同口 Exec 排队

| 字段 | 内容 |
|------|------|
| ID | TC-FW-SG-031 |
| Priority | P0 |
| ExistingTest | `FourWayHoistE2ETests` |

---

### TC-FW-SG-032 Hoist Feedback≠OK

| 字段 | 内容 |
|------|------|
| ID | TC-FW-SG-032 |
| Priority | P1 |
| Type | Negative |
| ExistingTest | `FourWayHoistE2ETests`（BadFeedback） |

---

## E. 运维与联锁

### TC-FW-SG-040 监控看板（FloorPlan）

| 字段 | 内容 |
|------|------|
| ID | TC-FW-SG-040 |
| Priority | P1 |
| Automation | Browser / ApiHttp |
| UiTestId | `ops-floorplan` |
| Gap | G-FW-05 |

---

### TC-FW-SG-041 急停拒接单

| 字段 | 内容 |
|------|------|
| ID | TC-FW-SG-041 |
| Priority | P0 |
| Automation | ApiHttp / DotNet |

---

### TC-FW-SG-042 ForceComplete 释资源（缺口）

| 字段 | 内容 |
|------|------|
| ID | TC-FW-SG-042 |
| Priority | P2 |
| Type | Gate |
| Gap | G-FW-08 |
| Note | 修后再升 P0 |

---

## F. 地图转换

### TC-FW-SG-050 simproj → fixture

| 字段 | 内容 |
|------|------|
| ID | TC-FW-SG-050 |
| Priority | P2 |
| Automation | Script |

**期望** L01/L02 与文档 §0 同量级（~28/30 · ~26/26）。

---

## 覆盖矩阵

| 能力 | 用例 | DotNet |
|------|------|--------|
| Dijkstra | 001 | ✅ Singapore |
| 占边 | 002 | ✅ Singapore |
| 选图 | 003 | ✅ Singapore |
| 分配 | 010–012 | ✅ |
| 停车/Pri | 020–022 | ✅ |
| Hoist | 030–032 | ✅ |
| Ops | 040–041 | 活库/Browser |
| ForceComplete 释资源 | 042 | Gate |
