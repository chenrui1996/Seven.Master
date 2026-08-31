# 02 · 平台架构与 Features

## 1. 总体架构（画给面试官看）

```text
ERP/MES / Vue3 运维 / PDA / Simulator
                 │
                 ▼
            Seven.WebApi
     ┌───────────┼───────────┐
     │           │           │
   系统域       WMS        编排总线 Bus_*
 (用户菜单      账本         │
  工作流…)    Wms_*         ▼
                      IWcsPack[]（可插拔）
                   ┌────┴────┐
                Stacker   FourWay  (External…)
                 Stk_*     Fw_*
                   │         │
                   └────┬────┘
                        ▼
            IEquipmentTriggerPort
         （默认 InMemory 仿真；Phase H → DeviceComm/Socket）
```

**产品形态一句话**：单进程 = **WMS 账本** + **薄编排总线** + **厚 WCS 包**。

---

## 2. 启动与 DI（要点）

入口：`Seven.WebApi/Program.cs` + `Infrastructure/DependencyInjection.cs` → `AddSevenInfrastructure`。

典型注册顺序（口述即可）：

1. Options（Database / Jwt / Features / HotStore…）
2. DbContext + 审计拦截器
3. Cache（Memory/Redis + 延迟双删）
4. HotStore / DeviceComm（按开关）
5. 平台服务 + **`AddSevenWcs`（始终注册）** + WMS / Scada / Simulator
6. JWT + Permission Handler
7. MQ；Quartz 真实现或 Stub
8. Outbox HostedService（需 MQ+Outbox）
9. 文件存储

再挂：`AddSevenBusiness`、Builder、SignalR、Health、Swagger 分组。

### 中间件管道（责任链顺序有意义）

```text
Metrics → Exception → TraceId → RequestLog → IpWhitelist
→ [Idempotency] → Compression → RateLimiter
→ Static/CORS → AuthN → AuthZ → 租户 Claim→DbContext
→ Controllers / Hubs / health / metrics
→ DbSeeder（非 Testing）
```

**面试追问「为什么 Exception 最前」**：保证后续任意失败都能统一成 `WebResponseContent`，并带 TraceId。

---

## 3. Features：本项目最重要的平台亮点之一

配置节 `Features`，两类开关**语义不同**——面试必分清。

### 3.1 基础设施类（关 = 真卸或 Stub）

| 开关 | 关闭效果 |
|------|----------|
| WorkFlow / Quartz / MessageQueue / HotStore / DeviceComm / Alarm / SignalR / Tenant… | API 404、Stub 服务、不启 HostedService、不 MapHub |
| 多数与细节节 **AND** | 如 `Features.MinIO && MinIO:Enabled` |

Controller 上 `[RequiresFeature("WorkFlow")]`：关闭直接 404。

### 3.2 模块 UI 类（关 = 只藏菜单）

| 开关 | 关闭效果 |
|------|----------|
| `Wms` / `OrchestrationBus` / `WcsPacks.Stacker` / `FourWay`… | **仅前端菜单隐藏** |
| 后端 | 表、DI、业务 API、包服务 **始终保留** |

前端：`Seven.Vue3/src/stores/features.ts` 启动拉 `GET /api/config/features` 过滤菜单。

### 为什么这样设计？（亮点话术）

1. **避免「关个菜单把库内任务调度卸掉」** 导致线上半残。
2. **同一代码库服务多种交付形态**：纯账本 UI、仅堆垛、仅四向、双包同仓。
3. **联调/仿真仍可打隐藏模块的 API**，运维用菜单裁剪即可。

---

## 4. 平台横切能力清单（知道职责即可）

| 模块 | 核心价值 | 开关 |
|------|----------|------|
| WorkFlow | 线性审批；业务表 `AuditStatus` | WorkFlow |
| Quartz | 定时任务；关则 Disabled 占位 | Quartz |
| MQ | MassTransit；告警命令等；Outbox 可选 | MessageQueue (+Outbox) |
| Alarm | 码表 + Raise + SignalR Hub | Alarm |
| Builder | 表结构→Entity/Service/Controller/Vue | Builder（常默认开） |
| Health/Metrics | K8s 探活 + Prometheus | 常开 |
| 多租户 | `TenantId` QueryFilter；**无**多库/动态连接串 | Tenant |
| 幂等/限流/IP 白名单 | 安全横切 | 各自 Features |

---

## 5. 代码生成器（面试怎么定位）

- 元数据：`Sys_TableInfo` / `Sys_TableColumn`
- 产出：Domain 实体、Infrastructure Service、`Controllers/Generated`、Vue 页 + `extension` hooks
- **生成页扩展**：工具栏/行内按钮、列设置、主子表——见 doc/11、12
- **话术**：用生成加速 CRUD；WCS 调度/寻路等**手写核心**，不指望生成器产出状态机

---

## 6. 架构决策复盘（来自 design/）

| 决策 | 选择 | 放弃的选项 | 原因 |
|------|------|------------|------|
| 进程边界 | 单进程 WMS+WCS | 独立 WCS 微服务起步 | 共享库位、降部署复杂度；包边界已隔离设备差异 |
| 库位模型 | 统一 `Wms_*` + Pack 前缀 | 每包一套 Location 表 | RCS 裁定：共表可行；Layer 给四向一等公民 |
| 跨包协作 | 总线 Leg + 交接位 | 统一超级路网 | 避免包内算法互相污染 |
| 设备接入 | TriggerPort 语义口 | 业务直接解 S7/Modbus | 仿真与真机同路径；Phase H 可替换 |
| 热路径 | HotStore | Cache-Aside 打流量表 | 节拍写放大不可接受 |

---

## 7. 明确未做（诚实边界 = 加分）

- OAuth/OIDC、配置中心、独立 Repository
- 多租户多库 / Schema 隔离
- Phase H：真机通讯包替换 TriggerPort、多活调度写者
- 四向 F6 Promote / Simulator 生产化联调完整闭环
- PDA 拣选发货离线；堆垛 SupperRoute 时间窗等

面试官问「还缺什么」时，用「已规划 Phase / 刻意延后」表述，体现产品节奏感。
