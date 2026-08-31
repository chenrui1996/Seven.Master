# Seven.Master 面试要点全集

本目录把项目实现中的**要点、难点、亮点、技术栈**收敛成可背诵、可深挖的面试材料。目标：读完即可讲清「这是什么系统、怎么分层、WMS/WCS 怎么跑、为什么这样设计」。

配套实现细节仍以 [20-WMS与WCS实现说明](../20-WMS与WCS实现说明.md)、[19-WMS与WCS包](../19-WMS与WCS包.md) 为准；本目录偏**面试叙事与决策复盘**。

---

## 阅读顺序（建议 2～3 小时）

| 顺序 | 文档 | 面试用途 |
|------|------|----------|
| 1 | [01-技术栈与工程结构](./01-技术栈与工程结构.md) | 开场自我介绍、技术栈对答 |
| 2 | [02-平台架构与Features](./02-平台架构与Features.md) | 架构题、可裁剪平台题 |
| 3 | [03-安全权限与横切](./03-安全权限与横切.md) | 认证授权、中间件、运维能力 |
| 4 | [04-HotStore与DeviceComm](./04-HotStore与DeviceComm.md) | **高频深挖**：热数据 ≠ 缓存 |
| 5 | [05-WMS账本与总线](./05-WMS账本与总线.md) | 业务边界、三层单据、库存一致性 |
| 6 | [06-立库WCS难点亮点](./06-立库WCS难点亮点.md) | 双深、深浅移库、Dijkstra 拆腿 |
| 7 | [07-四向车WCS难点亮点](./07-四向车WCS难点亮点.md) | 占边、停车、Hoist 三阶段 |
| 8 | [08-面试问答与讲述稿](./08-面试问答与讲述稿.md) | 30s / 3min / 追问清单 |

---

## 一句话产品定位（背下来）

> Seven 是从 VolCore/Legrand 迁移的 **.NET 8 + Vue 3 企业后台**，通过 **Features 裁剪** 从 CRUD 平滑扩展到 **单进程 = WMS 账本 + 薄编排总线 + 可插拔 WCS 包（堆垛 / 四向）**；设备侧默认仿真 TriggerPort，真机 Phase H 替换同一接口。

---

## 亮点速览（面试开场可点名 3～5 个）

1. **Features 分两类**：基础设施关真卸能力；WMS/WCS 包仅藏菜单、服务始终注册。
2. **统一 `Wms_*` 库位账本 + Pack 前缀隔离**，同仓多设备族不裂库存。
3. **薄总线 + 厚包**：跨包不合并路网，靠交接位与 `Bus_TransportLeg.PackId`。
4. **SUDR 语义统一仿真与真机**：业务不解析 PLC 报文。
5. **HotStore**：路径占道真相源 + 异步落库，与 CRUD 延迟双删缓存职责分离。
6. **立库双深 / 深浅移库**、**四向层内占边 + 停车原子预订 + Hoist 包内三阶段**。
7. **大量命名良好的 E2E 测试**（InboundToStacker、StackerDoubleDeep、FourWayHoist…）。

---

## 难点速览（挑 1～2 个准备深挖）

| 域 | 难点 | 详见 |
|----|------|------|
| 平台 | HotStore 与 Cache 边界；单写者约定 | [04](./04-HotStore与DeviceComm.md) |
| WMS | 预留 AvailableQty vs 正式 Ship；同址平库不建运 | [05](./05-WMS账本与总线.md) |
| 立库 | 防孤二深 Booking；挡路深位 Pri 抬升 + Transfer | [06](./06-立库WCS难点亮点.md) |
| 四向 | 跨层不拆多段总线；同口 Hoist 联锁；停车 Free→Reserved→Occupied | [07](./07-四向车WCS难点亮点.md) |

---

## 仓库与文档地图

```
Seven.Master/
├── Seven.Net8/       后端（WebApi 启动）
├── Seven.Vue3/       运维前端
├── Seven.App/        WMS PDA（uni-app）
├── Seven.Simulator/  联调 SPA（Features→地图→仿真→Promote）
├── design/           架构裁定原稿
├── doc/              开发文档 01–20
└── doc/keypoint/     ← 本目录
```
