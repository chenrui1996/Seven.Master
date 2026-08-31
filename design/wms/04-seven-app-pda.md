# Seven.App：LesApp → WMS PDA 迁移方案

状态：M5 已落地（盘点 PDA S4 + Vue3 运维页）；后续 M6 待实施  
日期：2026-08-29  
源：`D:\Junheinrich\Project\FJD\SrcCode\LES2\LesApp`（**.NET MAUI 8**，永恒力 WMS PDA）

---

## 1. 目标形态

| 项 | 选择 |
|----|------|
| 工程路径 | `Seven.Master/Seven.App/` |
| 技术栈 | **uni-app（Vue3 + TypeScript）+ Pinia**，对齐 `Seven.Vue3` 生态；Android PDA 优先 |
| 后端 | Seven.WebApi 新建 **`/api/pda/*`**；**不**包装 LES `/api/App` |
| 鉴权 | 复用 Seven JWT；扩展 warehouse / pda 菜单 claim |
| 离线 | 首期**在线**（与 LesApp 一致；「离线拣选」仅为业务名） |

---

## 2. LesApp 能力映射

| LesApp | Seven.App 页面 | Seven API（新建） |
|--------|----------------|-------------------|
| Login / JWT / 选仓 | `pages/login` | `/api/Auth/*` + `/api/pda/warehouses` |
| Feature 宫格 + LoadNodeTree | `pages/home` | `/api/pda/menu`（client=pda） |
| `*Receiving4Floor` | `pages/receive/*` | `/api/pda/inbound/*/receive` |
| `Putaway4Floor` | `pages/putaway/*` | `/api/pda/putaway/*` |
| `FloorPick` / OfflinePick | `pages/pick/*` | `/api/pda/pick/*` |
| `*OutboundShipping4Floor` / Waybill | `pages/ship/*` | `/api/pda/outbound/*` |
| `CycleCount4Floor` | `pages/cyclecount/*` | `/api/pda/cyclecount/*` |
| 移库 / 拆托 / 空托 / 查询 | `pages/inventory/*`、`query/*` | 对应 pda 服务 |

立库页：收货组盘后主要等自动化；PDA 保留组盘/异常收/取消码垛，上架确认以平库为主。

---

## 3. 工程结构（建议）

```text
Seven.App/
  package.json          # uni-app vue3
  src/
    api/                # luch-request / axios 适配
    stores/             # user, flow(收货/拣选会话)
    pages/
      login / home / receive / putaway / pick / ship / cyclecount / query
    components/         # 扫码壳、Busy
  README.md
```

配置：`VITE_API_BASE_URL` → Seven.WebApi。

---

## 4. 后端 PDA 面

新建 Application 契约 + Controller，薄封装现有 WMS 服务：

- 输入输出面向扫码：容器码、库位码（**已含 Pack 前缀**）、数量。
- 推荐库位：调用目标 `IWcsLocationAllocator`（按仓库 EnabledPackIds / 用户选区 Pack）。
- 权限：`Pda.Receive` 等；菜单种子 `SystemType=PDA`。

---

## 5. 实施阶段

| 步 | 内容 |
|----|------|
| S0 | 建 `Seven.App` uni-app 脚手架、登录、宫格、代理 | ✅ |
| S1 | 平库收货竖切（一单种） | ✅ |
| S2 | 平库上架 | ✅ |
| S3 | 拣选 + 发货确认 | |
| S4 | 盘点录入 | ✅ |
| S5 | 立库组盘/查询；扫码枪广播适配 | |
DTO：从 LES `ClientModel` **重写为 TypeScript**，字段对齐 Seven 实体，不共享 LES.Entities。

---

## 6. 非目标（首期）

- 移植 MAUI / UraniumUI
- SecureStorage 争议（用 uni.storage 即可）
- HA `X-Les-Route`（可后置）
- 真离线队列

---

## 7. 参考路径

```
LES2\LesApp\HttpClients\WmsApiClient.cs
LES2\LesWebCore\Controllers\AppController.cs
LES2\LES.Entities\LES\ClientModel\**
LES2\Doc\平库\**\*菜单*.sql
```
