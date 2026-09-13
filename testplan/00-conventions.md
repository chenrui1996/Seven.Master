# 测试约定：用例模板、自动化分层与钩子登记

---

## 1. 用例 ID 规则

| 前缀 | 域 |
|------|-----|
| `TC-WMS-*` | WMS 账本/单据/PDA |
| `TC-SRM-*` | 立库堆垛 WCS |
| `TC-FW-*` | 四向穿梭车 WCS |
| `TC-X-*` | 跨包 / 运维 IA / 联锁 |
| `TC-OPS-STK-*` | 立库运维 API/UI（可与 SRM 交叉引用） |
| `TC-OPS-FW-*` | 四向运维 API/UI |

优先级：`P0` 发布阻断 · `P1` 高 · `P2` 中 · `P3` 低。

---

## 2. 标准用例字段

```markdown
### TC-XXX-000 标题

| 字段 | 内容 |
|------|------|
| ID | TC-XXX-000 |
| Priority | P0 |
| Type | HappyPath / Negative / Gate / UI / Regression |
| Features | STK / FW / DUAL / WMS |
| Automation | DotNet \| ApiHttp \| Browser \| Manual （可多选，主路径写第一） |
| Hook | `hook:domain.action` |
| ExistingTest | 类名.方法名 或 `—` |
| FutureTest | 建议测试类/方法名（脚本预留） |
| UiTestId | 建议 `data-testid`（浏览器预留） |
| API | 方法 + 路径 |

**前置**  
...

**步骤**  
1. ...

**期望**  
- ...

**异常/边界**  
- ...

**数据清理**  
- ...
```

---

## 3. Automation 取值说明

| 值 | 何时标 | 实现建议 |
|----|--------|----------|
| **DotNet** | 纯服务/包/分配/状态机，不依赖浏览器 | `Seven.Tests` xUnit + InMemory |
| **ApiHttp** | 需鉴权、路由、Permission、序列化 | `WebApplicationFactory` 或 RestClient |
| **Browser** | 菜单可见性、布局、点击链路、testid | Playwright；`@tag` = Hook 短名 |
| **Manual** | 现场设备、PLC、目视 SCADA | 检查表 |

一案可多标，例如：`Automation: DotNet, ApiHttp`（服务先覆盖，HTTP 后补）。

---

## 4. Hook 命名

`hook:<pack|wms|bus|ops|ui>.<resource>.<action>`

示例：

- `hook:wms.inbound.buildPallet`
- `hook:stk.trigger.destinationRequest`
- `hook:fw.ops.forceComplete`
- `hook:ui.menu.fwOpsVisible`

脚本侧建议：

```csharp
// DotNet / ApiHttp
[Fact]
[Trait("Hook", "hook:fw.ops.forceComplete")]
public async Task ForceComplete_Shuttle_ShouldCompleteLeg() { ... }
```

```ts
// Playwright 预留
test('TC-OPS-FW-009 force-complete @hook:fw.ops.forceComplete', async ({ page }) => {
  await page.getByTestId('fw-ops-force-complete').click()
})
```

---

## 5. 钩子登记表（权威清单）

### 5.1 WMS API

| Hook | HTTP | 权限/备注 |
|------|------|-----------|
| `hook:wms.inbound.add` | `POST /api/WmsInboundOrder/add` | |
| `hook:wms.inbound.approve` | `POST /api/WmsInboundOrder/approve/{id}` | |
| `hook:wms.inbound.buildPallet` | `POST /api/WmsInboundOrder/buildPallet/{id}` | |
| `hook:wms.inbound.receive` | `POST /api/WmsInboundOrder/receive/{id}` | |
| `hook:wms.outbound.add` | `POST /api/WmsOutboundOrder/add` | |
| `hook:wms.outbound.approve` | `POST /api/WmsOutboundOrder/approve/{id}` | |
| `hook:wms.outbound.ship` | `POST /api/WmsOutboundOrder/ship/{id}` | |
| `hook:wms.outbound.generatePicks` | `POST /api/WmsOutboundOrder/generatePicks/{id}` | |
| `hook:wms.cyclecount.add` | `POST /api/WmsCycleCount/add` | |
| `hook:wms.cyclecount.record` | `POST /api/WmsCycleCount/record/{id}/{lineNo}` | |
| `hook:wms.cyclecount.confirm` | `POST /api/WmsCycleCount/confirm/{id}` | |
| `hook:wms.pda.menu` | `GET /api/pda/menu` | JWT |
| `hook:wms.pda.inbound` | `/api/pda/inbound/*` | |
| `hook:wms.pda.putaway` | `/api/pda/putaway/*` | |
| `hook:wms.pda.cyclecount` | `/api/pda/cyclecount/*` | |

### 5.2 Trigger / Bus

| Hook | HTTP |
|------|------|
| `hook:wcs.trigger.destinationRequest` | `POST /api/Wcs/Triggers/destination-request` |
| `hook:wcs.trigger.segmentFeedback` | `POST /api/Wcs/Triggers/segment-feedback` |
| `hook:bus.order.query` | `POST /api/BusTransportOrder/getPageData` |
| `hook:bus.leg.query` | `POST /api/BusTransportLeg/getPageData` |

### 5.3 FourWay Ops

| Hook | HTTP |
|------|------|
| `hook:fw.ops.meta` | `GET /api/Wcs/FourWay/Ops/meta` |
| `hook:fw.ops.board` | `GET /api/Wcs/FourWay/Ops/board` |
| `hook:fw.ops.taskTree` | `GET /api/Wcs/FourWay/Ops/task-tree` |
| `hook:fw.ops.inbound` | `POST /api/Wcs/FourWay/Ops/inbound` |
| `hook:fw.ops.pickableMap` | `GET /api/Wcs/FourWay/Ops/inbound/pickable-map` |
| `hook:fw.ops.pointDispatch` | `POST /api/Wcs/FourWay/Ops/point-dispatch` |
| `hook:fw.ops.charge` | `POST /api/Wcs/FourWay/Ops/charge` |
| `hook:fw.ops.chargeStop` | `POST /api/Wcs/FourWay/Ops/charge/stop` |
| `hook:fw.ops.forceComplete` | `POST /api/Wcs/FourWay/Ops/force-complete` | Body: `{ targetType, id }`；targetType=`shuttle\|hoist\|…` |
| `hook:fw.ops.resend` | `POST /api/Wcs/FourWay/Ops/resend` | |
| `hook:fw.ops.controlMode` | `GET\|POST /api/Wcs/FourWay/Ops/control-mode` | POST `mode` 为 **数值枚举**（0=Auto,1=Manual） |

### 5.4 Stacker Ops

| Hook | HTTP |
|------|------|
| `hook:stk.ops.board` | `GET /api/Wcs/Stacker/Ops/board` |
| `hook:stk.ops.taskTree` | `GET /api/Wcs/Stacker/Ops/task-tree` |
| `hook:stk.ops.forceComplete` | `POST /api/Wcs/Stacker/Ops/force-complete` | Body: `{ targetType: putAway\|retrieval\|device, id }` |
| `hook:stk.ops.resend` | `POST /api/Wcs/Stacker/Ops/resend` |
| `hook:stk.ops.requestPoints` | `GET /api/Wcs/Stacker/Ops/request-points` |
| `hook:stk.ops.requestPointEnable` | `POST .../request-point/{id}/enable\|disable` |
| `hook:stk.ops.controlMode` | `GET\|POST /api/Wcs/Stacker/Ops/control-mode` |

### 5.5 Platform / 配置

| Hook | HTTP |
|------|------|
| `hook:ctl.mode.get` | `GET /api/ControlMode/{scope}` |
| `hook:ctl.mode.set` | `POST /api/ControlMode/{scope}/mode` |
| `hook:ctl.estop.set` | `POST /api/ControlMode/{scope}/estop` |
| `hook:ifc.log.page` | `POST /api/InterfaceLog/getPageData` |
| `hook:cfg.features` | `GET /api/config/features` |
| `hook:sys.menu` | 登录后 `getMenu` |

### 5.6 UI data-testid（未来补到 Vue）

| UiTestId | 建议挂载 |
|----------|----------|
| `wms-inbound-approve` | `InboundOrder.vue` 审核 |
| `wms-inbound-build-pallet` | 组盘提交 |
| `wms-outbound-approve` | 出库审核 |
| `wms-outbound-ship` | 发运 |
| `wms-cyclecount-confirm` | 盘点确认 |
| `stk-ops-monitor` | `/Wcs/Stacker/Ops/Monitor` 根 |
| `stk-ops-force-complete` | Srm 强制完成 |
| `stk-ops-resend` | Srm 重发 |
| `stk-ops-request-disable` | 申请点停用 |
| `fw-ops-monitor` | 四向监控根 |
| `fw-ops-inbound-submit` | 运维入库提交 |
| `fw-ops-point-dispatch` | 指定点 |
| `fw-ops-charge` / `fw-ops-charge-stop` | 充电 |
| `fw-ops-force-complete` | 穿梭强制完成 |
| `fw-ops-resend` | 重发 |
| `ops-estop-pack` / `ops-estop-global` | 联锁页 |
| `rail-menu-wcsops` | 不应存在（负向） |
| `rail-menu-fw-ops` / `rail-menu-stk-ops` | 一级轨下运维目录 |
| `wcs-trigger-dest-submit` | Trigger 页目的地申请 |
| `wcs-trigger-seg-submit` | Trigger 段反馈 |

---

## 6. FutureTest 类名建议（脚本脚手架）

```
Seven.Tests/Wcs/FourWayOpsServiceTests.cs
Seven.Tests/Wcs/StackerOpsServiceTests.cs
Seven.Tests/Wcs/FourWayOpsApiTests.cs      // WebApplicationFactory
Seven.Tests/Wcs/StackerOpsApiTests.cs
Seven.Tests/Wms/WmsHttpE2ETests.cs
Seven.Vue3/e2e/ops-menu.spec.ts           // Playwright 预留
Seven.Vue3/e2e/wcs-ops-shuttle.spec.ts
```

Trait 约定：`Hook`、`CaseId`、`Priority`。

---

## 7. 主数据夹具约定（可重复）

| 键 | 含义 |
|----|------|
| `WH-DEMO` | 演示仓，`EnabledPackIds=stacker,fourway` |
| `Stk.RCV-01` | 堆垛收货/申请点 |
| `Stk.A-01-…` | 堆垛货位 |
| `Fw.GW-L1` | 四向 Gateway / ShuttleEp |
| `Fw.L01` / `Fw.L02` | 层 |
| `Fw.BIN-…` | 四向货位 |
| `CTR-T###` | 容器号流水 |

每个 DotNet 用例应自建夹具或使用 TestFixture，禁止依赖脏库偶然数据。
