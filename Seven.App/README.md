# Seven.App（WMS PDA）

uni-app（Vue3 + TypeScript + Pinia），对接 Seven.WebApi `/api/pda/*`。

## 本机运行（H5）

```bash
# 先启动 Seven.WebApi（默认 http://localhost:5000）
cd Seven.App
npm install
npm run dev:h5
```

浏览器打开控制台提示的地址（通常 `http://localhost:5174`）。

配置：`.env.development` 中 `VITE_API_BASE_URL`。

## 竖切流程（M4–M5）

1. 登录（`/api/Auth/login`，开发环境若开验证码需先关或传码）
2. 首页宫格 → **平库收货** / **平库上架** / **盘点录入**
3. 收货：选单 → 扫容器 + 收货位 → `POST /api/pda/inbound/{id}/receive`
4. 上架：选待上架容器 → 扫目标库位 → `POST /api/pda/putaway/confirm`
5. 盘点：选单 → 扫库位录入实盘 → 全部已盘后调账 → `POST /api/pda/cyclecount/{id}/record|confirm`

Android PDA：用 HBuilderX 打自定义基座，或 `dev:app`（需 App 资源）；扫码枪按键盘楔入即可填输入框。

## API

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/pda/menu` | 宫格菜单 |
| GET | `/api/pda/inbound/pending` | 待收货单 |
| POST | `/api/pda/inbound/{id}/receive` | 平库收货组盘 |
| GET | `/api/pda/putaway/pending` | 待上架 |
| POST | `/api/pda/putaway/confirm` | 平库上架确认 |
| GET | `/api/pda/cyclecount/pending` | 待盘点单 |
| GET | `/api/pda/cyclecount/{id}` | 盘点明细 |
| POST | `/api/pda/cyclecount/{id}/record` | 实盘录入（行号或库位） |
| POST | `/api/pda/cyclecount/{id}/confirm` | 差异调账 |
