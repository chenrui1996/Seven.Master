# 生成页多选与扩展按钮 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 生成 CRUD 页默认支持行多选与批量删除，并通过不覆盖的扩展文件声明工具栏自定义按钮。

**Architecture:** VuePage 模板内建 selection + `selectedRows`；动态 import `src/extension/{folder}/{Table}.ts`；Builder 仅在扩展文件不存在时写入空模板。

**Tech Stack:** Vue 3 + Element Plus + TypeScript；Seven.Builder 代码生成

---

### Task 1: 扩展类型定义

**Files:**
- Create: `Seven.Vue3/src/extension/types.ts`
- Create: `Seven.Vue3/src/extension/empty.ts`（可选默认空扩展）

**Step 1:** 定义 `PageActionContext`、`ToolbarButton`、`PageExtension`

**Step 2:** 确认可被扩展文件与生成页 import

---

### Task 2: 更新 VuePage 模板

**Files:**
- Modify: `Seven.Net8/Seven.WebApi/Template/VuePage.html`

**Step 1:** 工具栏改为 `toolbar-actions`：扩展按钮槽位 + 批量删除 + 新增

**Step 2:** 表格增加 `type="selection"` 与 `@selection-change`

**Step 3:** script 增加 `selectedRows`、`onSelectionChange`、`batchRemove`、`pageCtx`、动态加载扩展 `toolbarButtons`

**Step 4:** 扩展 import 使用占位符 `{{ExtensionImport}}`（相对路径）

---

### Task 3: Builder 生成扩展文件

**Files:**
- Modify: `Seven.Net8/Seven.Builder/BuilderService.cs`
- Modify: `Seven.Net8/Seven.Builder/ProjectPath.cs`（如需 `VueExtensionPath`）

**Step 1:** `CreateVuePageAsync` 计算 `extension/{folder}/{Table}.ts` 路径

**Step 2:** 若文件不存在，写入空扩展模板（含类型与示例注释）

**Step 3:** tokens 增加 `ExtensionImport`（如 `../../../extension/Business/Device`）

**Step 4:** 已存在则跳过，日志/返回消息中提示「已保留扩展文件」

---

### Task 4: 语言包

**Files:**
- Modify: `Seven.Vue3/src/locales/lang/zh-CN.json`
- Modify: `Seven.Vue3/src/locales/lang/en-US.json`
- Modify: `Seven.Vue3/src/locales/lang/ja-JP.json`

**Step 1:** `common.batchDelete`、`common.selectRequired`（未选中提示）

---

### Task 5: 落地 Device 示例

**Files:**
- Create: `Seven.Vue3/src/extension/Business/Device.ts`
- Modify: `Seven.Vue3/src/views/Business/Device.vue`

**Step 1:** Device 扩展文件（空 `toolbarButtons` + 注释示例：如何 post / router.push）

**Step 2:** Device.vue 与模板行为对齐（selection、批量删除、扩展按钮）

**Step 3:** 浏览器验证：多选、批量删除、扩展按钮 disabled 规则

---

### Task 6: 验证生成路径

**Step 1:** 确认 WebApi 重启后「生成 Vue」会创建扩展文件且不覆盖已有

**Step 2:** 确认生成页 import 路径在 `views/Business` 与 `views/system` 下均正确

---

## 完成标准

- [ ] 生成页可选中行并批量删除
- [ ] 扩展文件按钮可拿到选中数据
- [ ] 再生成不覆盖扩展文件
