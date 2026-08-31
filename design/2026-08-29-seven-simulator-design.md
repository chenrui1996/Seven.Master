# Seven.Simulator：仿真工程 → 生产上线

> **定稿规格（完整版）：** [`docs/superpowers/specs/2026-08-29-seven-simulator-design.md`](../docs/superpowers/specs/2026-08-29-seven-simulator-design.md)  
> **开发计划：** [`docs/superpowers/plans/2026-08-29-seven-simulator.md`](../docs/superpowers/plans/2026-08-29-seven-simulator.md)  
> **实施流程（doc）：** [`doc/21-仿真器与联调闭环.md`](../doc/21-仿真器与联调闭环.md)

状态：已定稿（2026-08-29）  
决策：**方案 A**（Vue3 能力全量重写）+ **路径 1**（闭环优先，一期→四期顺序执行）  
参考：`simulation-spa` 迁能力与契约，**不引** RCS/LES 程序集

---

## 闭环

```text
编辑地图 → Deploy（Wms_/Stk_|Fw_/Scd_，仓 SIM_*）
        → 仿真（一期 Trigger；三期 Gateway TCP+SignalR）
        → Promote（真机 IP + DeviceComm/通讯包；禁止只改 IP）
```

## 四期

| 期 | 交付 |
|----|------|
| I | Features + JSON/简易地图 + Deploy 增强 + Trigger Player + Promote |
| II | 完整 2D 编辑器（设备库/连线/拓扑） |
| III | Three.js + Gateway 协议保真 |
| IV | `.simproj` 适配、Excel、路径组 |

## 硬规则

1. 工程文件 ≠ 运行库（仅 Deploy 写库）  
2. 未 Promote 不得用仿真身份冒充生产  
3. 包隔离写入；库位权威仅 `Wms_Location`  
4. `Features.Simulator` 只控入口，Deploy API 始终可用  

细节、API、工程 JSON schema 以 **docs/superpowers/specs** 与 **doc/21** 为准。
