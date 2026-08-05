# 标签页右键菜单设计

状态：已批准（方案 1：MainLayout 内嵌菜单）

## 目标

在 `MainLayout` 标签栏增加右键上下文菜单，提供刷新、关闭、关闭其它、全部关闭、当前页全屏，交互与视觉对齐参考截图。

## 现状

`Seven.Vue3/src/layout/MainLayout.vue`：

- 自定义 `.tabs-bar` / `.tab-item`，点击切换、关闭图标关闭（首页无关闭按钮）
- `useTabsStore` 仅有 `addTab` / `removeTab`，无批量关闭与刷新

缺口：无右键菜单；无法批量关标签；无法强制重挂载；无内容区全屏。

## 决策摘要

| 项 | 选择 |
|----|------|
| 实现位置 | `MainLayout.vue` 内嵌浮层菜单（方案 1） |
| 刷新 | 强制重挂载（换 key + 短暂移出 keep-alive include） |
| 全屏 | 内容区全屏：隐藏侧栏 + 顶栏，保留标签栏 |
| 首页 | 始终保留；首页上「关闭」禁用 |

## 交互

1. 任意标签 `@contextmenu.prevent` 弹出菜单；点击空白或选中项后关闭。
2. 菜单五项（图标 + 文案，hover 主题橙）：刷新、关闭、关闭其它、全部关闭、当前页全屏。
3. 操作对象为 **被右键的标签**；若非当前激活，刷新 / 关闭 / 全屏先切到该标签再执行。
4. 首页始终保留：「关闭」在首页上禁用；「关闭其它」「全部关闭」不关 `/home`。
5. 关闭当前页时：切到相邻可用标签（优先右，否则左，最终回首页）。标签上的 X 与菜单「关闭」共用同一关闭逻辑。
6. 关闭其它：只保留首页 + 目标页；若目标非当前页，关闭后切到目标页。
7. 全部关闭：只留首页并跳转 `/home`。
8. 全屏时菜单文案变为「退出全屏」；支持 Esc 退出。全屏状态不持久化。

## 实现要点

### 菜单 UI

- 绝对定位浮层，贴标签下方，必要时钳制在视口内。
- 图标固定为：`Refresh` / `Close` / `CircleClose` / `FolderDelete` / `FullScreen`（Element Plus Icons）。
- 文案走 i18n（zh-CN / en-US / ja-JP），键前缀 `layout.tabMenu.*`。

### Store（`useTabsStore`）

新增：

- `closeOthers(path)`：保留 `/home` 与目标 path，`activeTab = path`
- `closeAll()`：仅保留 `/home`，`activeTab = '/home'`

现有 `removeTab` 用于单关；`MainLayout` 在关当前页时按「右→左→首页」选择下一激活路径并 `router.push`。

### 强制刷新

- `MainLayout` 维护全局 `viewKey`（数字），绑在 `router-view` 内 component 的 `:key`（可与 `route.fullPath` 组合，如 `` `${route.fullPath}-${viewKey}` ``）。
- 刷新时：必要时先 `switchTab`，递增 `viewKey`，并短暂将该页 `componentName` 移出 keep-alive `include`（下一 tick 再加回），避免旧缓存回填。

### 内容区全屏

- `isContentFullscreen` 控制 class：隐藏 `el-aside` 与 `el-header`，主内容区占满；保留 `.tabs-bar`。
- `keydown` 监听 Esc 退出；组件卸载时移除监听。

## 改动范围

- `Seven.Vue3/src/layout/MainLayout.vue`：右键菜单、全屏 class、刷新 key、Esc
- `Seven.Vue3/src/stores/index.ts`：`closeOthers` / `closeAll`
- `Seven.Vue3/src/locales/lang/{zh-CN,en-US,ja-JP}.json`：菜单文案键

## 明确不做

- 不拆独立 `TabContextMenu.vue`（本轮）
- 不做浏览器 Fullscreen API
- 不做「关闭左边 / 关闭右边」（与旧 Legrand 菜单不同，以截图五项为准）
- 不持久化全屏状态
- 不改后端

## 验收

1. 右键出现五项，样式接近截图（图标 + hover 橙）
2. 刷新后页面状态重置（列表筛选 / 表单草稿消失）
3. 关闭 / 关闭其它 / 全部关闭行为正确，首页始终在
4. 全屏隐藏侧栏 + 顶栏；Esc 或再点「退出全屏」可恢复
5. 仅剩首页时关闭类操作无异常；菜单定位不溢出视口
