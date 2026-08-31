# 04 · HotStore 与 DeviceComm

> 这两块是平台层最容易被问穿的「工业软件味」能力。建议能手绘热冷分层图，并能对比 Cache。

---

## 一、HotStore（高频必问）

### 1. 要解决什么问题

WCS 调度节拍上：**边占用、节点锁、车辆状态** 每秒大量读写。

若用普通缓存 / 生成 CRUD：

| 反模式 | 后果 |
|--------|------|
| Get miss → 查库 → Set + 延迟双删 | 节拍打穿数据库 |
| 每拍 `SaveChanges` + 字段审计 | 写放大、锁竞争、日志爆炸 |
| 多实例无原子占用 | 双边同时占用 → 撞车 |

HotStore 定位：**热层 = 运行时真相源**；冷库保存地图版本、任务、占用快照；预热后运行期 **Get 未命中不回源**。

### 2. 架构图（背结构）

```text
启动: IHotStoreWarmup 装入热层 → IsReady=true
运行: 调度 → Get/Set/TryAcquire/Release → 热层
      TrackingHotStore 把脏变更丢进 Channel
      HostedService 按 PersistIntervalMs 批量 IHotStorePersister → DB
管理端低频 CRUD 改冷配置；监控 API 只读热层
```

### 3. 关键 API 语义

| API | 含义 |
|-----|------|
| `TryAcquireAsync` | 原子占用（边/资源） |
| `ReleaseAsync` | 释放 |
| `TryUpdateAsync` | 条件更新（Redis 上弱于 Acquire，强一致优先 Acquire） |
| `IsReady` | 预热完成前调度不应跑 |

### 4. Provider 与单写者

| Provider | 适用 | 风险 |
|----------|------|------|
| Memory | 单活调度进程 | 多实例状态分裂 |
| Redis + Lua | 多实例共享占用 | 运维成本；仍建议业务选主 |

`SingleWriter=true`：**框架假定单写者**；多活写者是明确延后项（Phase H / 运维选主）。

### 5. 与四向业务的约定（实现亮点）

- 生产键命名空间 **`fw:`**（边流 `fw:flow:` 等）
- **禁止**生产路径使用 Demo 全局 `wcs:`
- `HotStore:EnableDemoScheduler=false`
- Cancel/Fail 必须释尽该车占用边（path EdgeId + owner 索引）

### 6. 审计排除

落库热表短名命中 `AuditExcludeEntities` 时跳过字段审计——否则落库节拍会把审计表打爆。

### 7. 面试标准答法（30 秒）

> 菜单字典用 Cache + 延迟双删；路径占道用 HotStore。HotStore 预热后热层是真相，Acquire/Release 做互斥，脏数据异步批量落库，避免调度打 EF。四向用 `fw:` 命名空间，和 Demo 隔离。

### 8. 追问准备

- **落库失败怎么办？** 热层仍正确；需监控 Persister、对账/重放策略（实现以仓库为准，可答「优先保运行正确，冷层最终一致，运维可对账」）。
- **和 Redis 分布式锁区别？** 不只是锁，是带类型的热状态平面 + 变更追踪 + 批量持久化。
- **为何不直接用 EF 二缓？** 语义不对：二缓仍是缓存，不是「调度真相源 + 占用原语」。

---

## 二、DeviceComm（设备通讯）

### 1. 定位

可选模块：对接 **Siemens S7（S7netplus）** 与 **Modbus TCP（NModbus）**。

原则：

- 每连接 **串行 IO**（避免并发撕裂）
- 网关状态机 + **声明式规则引擎**（Monitor / WriteThenAwait / Sequence）
- 断连可抛告警码（如 DEV001）
- 可选把点位镜像进 HotStore
- `SingleWriter` 首期单活约定

### 2. 与 WCS 的关系（架构亮点）

当前 WCS **不直接**绑 DeviceComm。

```text
业务包 ──语义──► IEquipmentTriggerPort
                      │
          现在：InMemory（测试/运维 API 驱动 SUDR）
          将来 Phase H：Socket/S7/Modbus 实现同一接口
```

**业务代码不解析报文**——这是可测试性与可替换性的关键设计。

### 3. SUDR 语义对照（与立库/四向共用）

| 语义 | 端口方法（概念） |
|------|------------------|
| 目的地申请 | DestinationRequested / Simulate… |
| 下发目的地 | DispatchDestinationAsync |
| 拒收 | RejectDestinationAsync |
| 移动下发 | DispatchMoveAsync |
| 段完成反馈 | SegmentFeedback / Simulate… |

### 4. 双包共享 TriggerPort 的坑（已硬化）

外形校验 NG 时：若申请点属于**另一启用包**，本侧 **静默**，不要误 `Reject`——否则堆垛/四向互相「误杀」对方申请。

面试可说：「共享仿真口时用申请点归属做静默过滤，是联调实战踩坑后的硬化。」

### 5. 面试边界句

> DeviceComm 提供工业协议与规则 DSL；WCS 通过 TriggerPort 吃「申请/下发/反馈」语义。真机切换是换 Port 实现，不是改分配/寻路代码。
