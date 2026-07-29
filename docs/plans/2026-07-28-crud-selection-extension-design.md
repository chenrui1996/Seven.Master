# 生成页多选与扩展按钮设计

**日期：** 2026-07-28  
**状态：** 已确认  

## 背景

代码生成的 CRUD 页（如 `Device.vue`）缺少行多选与可扩展工具栏按钮。业务需要：勾选行后批量提交后台，或跳转自定义页并传入选中数据。重新生成 Vue 时不能覆盖业务扩展逻辑。

## 目标

1. 所有生成页默认支持表格复选框与选中状态。
2. 工具栏支持「批量删除」与来自扩展文件的自定义按钮。
3. 业务逻辑写在扩展文件中，重新生成 Vue **不覆盖**扩展文件。

## 方案概要

采用 **扩展文件（A）**，对齐 Legrand 的 extension 思路，但适配 Seven 当前「整页生成 SFC」模式（不做 view-grid）。

### 多选

- `el-table-column type="selection"`
- `@selection-change` → `selectedRows` / `selectedIds`（主键）
- 工具栏批量删除：未选中时 disabled；调用现有 `/api/{Table}/del`
- `loadData` 后清空选中

### 扩展文件

- 路径：`Seven.Vue3/src/extension/{Folder}/{TableName}.ts`（例：`extension/Business/Device.ts`）
- 首次生成：若文件不存在则创建空模板；若已存在则跳过
- 再次生成 Vue：只覆盖 `.vue`，扩展文件永不覆盖
- 生成页注释标明业务写在扩展文件

### 扩展 API

```ts
export interface PageActionContext {
  selectedRows: Record<string, unknown>[]
  selectedIds: number[]
  reload: () => Promise<void>
  http: typeof http
  router: Router
  t: (key: string) => string
}

export interface ToolbarButton {
  key: string
  label: string
  permission?: string
  type?: 'primary' | 'success' | 'warning' | 'danger' | 'default'
  requireSelection?: boolean
  onClick: (ctx: PageActionContext) => void | Promise<void>
}

export interface PageExtension {
  toolbarButtons?: ToolbarButton[]
}

export default { toolbarButtons: [] } satisfies PageExtension
```

自定义按钮典型用法：

- 提交后台：`http.post('/api/Device/xxx', ctx.selectedIds)` → `ctx.reload()`
- 打开页面：`ctx.router.push({ path: '...', query: { ids: ctx.selectedIds.join(',') } })`

### 第一期范围

- 做：多选、批量删除、工具栏扩展按钮、`PageActionContext`
- 不做：行内自定义按钮、gridHeader 组件挂载、复杂弹窗插槽（后续可扩展）

## 涉及文件

| 文件 | 变更 |
|------|------|
| `Template/VuePage.html` | selection、工具栏、扩展 import、批量删除 |
| `BuilderService.cs` | 首次写扩展文件；永不覆盖已有扩展 |
| `src/extension/types.ts` | 类型定义 |
| `src/extension/Business/Device.ts` | Device 示例扩展（空或示例按钮） |
| `views/Business/Device.vue` | 与模板对齐 |
| 语言包 `common` | `batchDelete` 等 |

## 成功标准

1. 生成页可选中多行，批量删除可用。
2. 扩展文件中声明的按钮出现在「新增」旁，能拿到 `selectedRows`/`selectedIds`。
3. 再次「生成 Vue」不丢失扩展文件中的自定义按钮逻辑。
