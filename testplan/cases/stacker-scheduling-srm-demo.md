# 立库调度场景用例（SRM Demo 地图）

约定：[`../00-conventions.md`](../00-conventions.md)  
逻辑：[`../../doc/keypoint/11-立库调度完整逻辑.md`](../../doc/keypoint/11-立库调度完整逻辑.md)（§8 差距矩阵）  
地图 fixture：[`../fixtures/srm-demo-stacker-map.json`](../fixtures/srm-demo-stacker-map.json)  
源 simproj：`20260426-SRM-Demo.simproj.json`

**种子约定**：灌入双深货位、`Stk_LocationProfile`、`Stk_Route/DeviceCoder`；货位码 `Stk.{coordPointId}`。

**差距标签**：`Gap:G-STK-xx` 对照文档 §8.2。

---

## A. 路径、拆腿、合并、段反馈

### TC-SRM-DEMO-001 Dijkstra + 容量边不可用

| 字段 | 内容 |
|------|------|
| ID | TC-SRM-DEMO-001 |
| Priority | P0 |
| ExistingTest | `StackerRouterTests.FindPath_ShouldPreferLowerWeight_AndSkipFullCapacity` |

---

### TC-SRM-DEMO-002 ExeStackCode 连续合并

| 字段 | 内容 |
|------|------|
| ID | TC-SRM-DEMO-002 |
| Priority | P0 |
| ExistingTest | `StackerPathDispatcherTests.Dispatch_ShouldMergeSameExeStack_AndAdvanceSegments` |

---

### TC-SRM-DEMO-003 无图/寻路失败单段退化

| 字段 | 内容 |
|------|------|
| ID | TC-SRM-DEMO-003 |
| Priority | P0 |
| ExistingTest | `StackerPathDispatcherTests.Dispatch_WithoutRoutes_ShouldFallbackSingleSegment` |

---

### TC-SRM-DEMO-004 DeviceCoder JudgeMap

| 字段 | 内容 |
|------|------|
| ID | TC-SRM-DEMO-004 |
| Priority | P1 |
| Automation | DotNet |
| Note | 间接覆盖于 PathDispatcher；专项可后补 |

---

### TC-SRM-DEMO-005 段反馈 FeedbackCode≠OK 不推进

| 字段 | 内容 |
|------|------|
| ID | TC-SRM-DEMO-005 |
| Priority | P0 |
| Type | Negative |
| Gap | G-STK-08（**已修**） |
| ExistingTest | `StackerPackTests.HandleSegmentFeedback_FeedbackCodeNg_ShouldNotAdvance` |

**期望** NG 后 DeviceTask 仍为 Dispatched；订单未 Completed；再 OK 后闭环完成。

---

## B. SUDR 分配与下发

### TC-SRM-DEMO-010 AisleRequest → 选巷 → Ep

| 字段 | 内容 |
|------|------|
| ID | TC-SRM-DEMO-010 |
| Priority | P0 |
| ExistingTest | `InboundToStackerE2ETests` / `StackerPackTests` |

---

### TC-SRM-DEMO-011 LocationRequest → Book 组预约

| 字段 | 内容 |
|------|------|
| ID | TC-SRM-DEMO-011 |
| Priority | P0 |
| ExistingTest | `StackerDoubleDeepTests.SelectLocation_ShouldBookSisters_InBinGroup` |

---

### TC-SRM-DEMO-012 BlockingPoint 重选

| 字段 | 内容 |
|------|------|
| ID | TC-SRM-DEMO-012 |
| Priority | P1 |
| Automation | DotNet |

---

## C. 双深

### TC-SRM-DEMO-020 拒绝孤二深入库

| 字段 | 内容 |
|------|------|
| ID | TC-SRM-DEMO-020 |
| Priority | P0 |
| ExistingTest | `StackerDoubleDeepTests.SelectLocation_ShouldSkipDeep_WhenShallowEmpty` |

---

### TC-SRM-DEMO-021 浅占后允许入深

| 字段 | 内容 |
|------|------|
| ID | TC-SRM-DEMO-021 |
| Priority | P0 |
| ExistingTest | `StackerDoubleDeepTests.SelectLocation_ShouldAllowDeep_WhenShallowOccupied` |

---

### TC-SRM-DEMO-022 DepthGuard 深浅移库

| 字段 | 内容 |
|------|------|
| ID | TC-SRM-DEMO-022 |
| Priority | P0 |
| Gap | G-STK-07（移库目标未走双深安全选位） |
| ExistingTest | `StackerDoubleDeepTests.DeepRetrieval_ShouldTransferShallowFirst_ThenDispatchDeep` |

---

### TC-SRM-DEMO-023 OutLockBin 强硬拒（缺口）

| 字段 | 内容 |
|------|------|
| ID | TC-SRM-DEMO-023 |
| Priority | P2 |
| Type | Gate |
| Gap | G-STK-06 |
| Note | 现状 OutLockBin 时 Guard 直接放行 |

---

## D. 高低货位 / 限高

### TC-SRM-DEMO-030 巷道 MaxHeight 过滤（已实现）

| 字段 | 内容 |
|------|------|
| ID | TC-SRM-DEMO-030 |
| Priority | P0 |
| ExistingTest | `StackerPackTests.SelectAisle_ShouldSkip_WhenHeightExceedsMaxHeight` |

---

### TC-SRM-DEMO-031 货位级高低匹配（缺口）

| 字段 | 内容 |
|------|------|
| ID | TC-SRM-DEMO-031 |
| Priority | P3 |
| Type | Gate |
| Gap | G-STK-02 |
| Automation | Manual |
| Note | fixture 有 cellHeight/elevation；产品未实现时 SKIP |

---

## E. Pri 与出库闭环

### TC-SRM-DEMO-040 同组 Pri 门闩

| 字段 | 内容 |
|------|------|
| ID | TC-SRM-DEMO-040 |
| Priority | P0 |
| ExistingTest | `OutboundToStackerE2ETests` |

---

### TC-SRM-DEMO-041 两巷两 SRM 并行不串巷

| 字段 | 内容 |
|------|------|
| ID | TC-SRM-DEMO-041 |
| Priority | P1 |
| Automation | DotNet / ApiHttp |

---

## F. 运维看板

### TC-SRM-DEMO-050 监控面板 FloorPlan

| 字段 | 内容 |
|------|------|
| ID | TC-SRM-DEMO-050 |
| Priority | P1 |
| Automation | Browser |
| UiTestId | `ops-floorplan` |

---

## G. 转换回归

### TC-SRM-DEMO-060 simproj → fixture

| 字段 | 内容 |
|------|------|
| ID | TC-SRM-DEMO-060 |
| Priority | P2 |
| Automation | Script |

**期望** locations=320；depth1=depth2=160；racks=4；srms=2。

---

## 覆盖矩阵

| 能力 | 用例 | DotNet |
|------|------|--------|
| Dijkstra/容量 | 001 | ✅ |
| ExeStack 合并 | 002 | ✅ |
| 单段退化 | 003 | ✅ |
| 段反馈 NG | 005 | ✅ 已修 |
| 分配/Booking | 010–011 | ✅ |
| 双深/DepthGuard | 020–022 | ✅ |
| OutLockBin | 023 | Gate |
| 巷限高 | 030 | ✅ |
| 货位高低档 | 031 | Gate |
| Pri | 040 | ✅ |
