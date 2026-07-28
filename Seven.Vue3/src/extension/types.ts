import type { Component } from 'vue'
import type { Router } from 'vue-router'

/** 工具栏/扩展按钮可访问的页面上下文 */
export interface PageActionContext {
  selectedRows: Record<string, unknown>[]
  selectedIds: number[]
  reload: () => Promise<void>
  http: {
    get: <T = unknown>(url: string) => Promise<T>
    post: <T = unknown>(url: string, data?: unknown) => Promise<T>
  }
  router: Router
  t: (key: string) => string
}

/** 工具栏自定义按钮 */
export interface ToolbarButton {
  key: string
  label: string
  /** Element Plus 图标组件，推荐使用 ActionIcons.xxx */
  icon?: Component
  permission?: string
  type?: 'primary' | 'success' | 'warning' | 'danger' | 'info' | 'default'
  /** 为 true 时未选中行则禁用 */
  requireSelection?: boolean
  onClick: (ctx: PageActionContext) => void | Promise<void>
}

/** 页面扩展（重新生成 Vue 不会覆盖扩展文件） */
export interface PageExtension {
  toolbarButtons?: ToolbarButton[]
}
