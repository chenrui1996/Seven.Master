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

/** 行内按钮上下文（含当前行；selectedRows/Ids 同步为该行便于复用逻辑） */
export interface RowActionContext extends PageActionContext {
  row: Record<string, unknown>
  rowId: number
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

/** 行内（操作列）自定义按钮 */
export interface RowButton {
  key: string
  label: string
  icon?: Component
  permission?: string
  /** link 按钮颜色，默认 primary */
  type?: 'primary' | 'success' | 'warning' | 'danger' | 'info' | 'default'
  /** 按行控制是否显示，默认显示 */
  visible?: (row: Record<string, unknown>) => boolean
  /** 按行控制是否禁用 */
  disabled?: (row: Record<string, unknown>) => boolean
  onClick: (ctx: RowActionContext) => void | Promise<void>
}

/** 查询字段配置（出现在列表上方搜索区） */
export interface SearchFieldConfig {
  prop: string
  /** 直接文案；优先于 labelKey */
  label?: string
  /** i18n key，如 generated.Device.deviceName */
  labelKey?: string
  kind?: 'string' | 'number' | 'enum' | 'date' | 'bool'
  /** 默认：string→like，其它→equal */
  operator?: 'equal' | 'like'
  options?: { value: number | string; label: string }[]
}

/** 主子表列 */
export interface DetailColumnConfig {
  prop: string
  label?: string
  labelKey?: string
  kind?: 'string' | 'number' | 'enum' | 'date' | 'bool'
  width?: number | string
}

/**
 * 主子表（一对多）
 * - below：选中主表行后在下方展示
 * - dialog：行内按钮（默认文案为 title）弹窗展示
 */
export interface DetailTableConfig {
  key: string
  /** 子表标题 / 弹窗标题 / 行内按钮默认文案 */
  title: string
  mode: 'below' | 'dialog'
  /** 行内按钮文案（仅 dialog），默认 title */
  buttonLabel?: string
  permission?: string
  /** 子表 API 路由名，走 getPageData；与 load 二选一 */
  apiRoute?: string
  /** 子表外键（camelCase），配合 apiRoute 过滤 */
  foreignKey?: string
  /** 主表主键字段，默认页面主键 */
  masterKey?: string
  pageSize?: number
  columns: DetailColumnConfig[]
  /** 自定义加载（优先于 apiRoute），适合 Demo / 聚合接口 */
  load?: (
    masterRow: Record<string, unknown>,
    ctx: PageActionContext,
  ) => Record<string, unknown>[] | Promise<Record<string, unknown>[]>
}

/** 钩子返回 false 则中断默认操作 */
export type CrudHookResult = boolean | void | Promise<boolean | void>

/** 拦截默认新增 / 修改 / 删除 */
export interface CrudHooks {
  /** 打开表单前 */
  beforeOpenForm?: (ctx: {
    mode: 'add' | 'edit'
    row?: Record<string, unknown>
    form: Record<string, unknown>
  }) => CrudHookResult
  /** 保存前（add/update） */
  beforeSave?: (ctx: {
    mode: 'add' | 'edit'
    form: Record<string, unknown>
  }) => CrudHookResult
  afterSave?: (ctx: {
    mode: 'add' | 'edit'
    form: Record<string, unknown>
  }) => void | Promise<void>
  /** 删除前（单行或批量） */
  beforeDelete?: (ctx: {
    ids: number[]
    rows: Record<string, unknown>[]
  }) => CrudHookResult
  afterDelete?: (ctx: { ids: number[] }) => void | Promise<void>
}

/** 页面扩展（重新生成 Vue 不会覆盖扩展文件） */
export interface PageExtension {
  toolbarButtons?: ToolbarButton[]
  /** 操作列自定义按钮（显示在编辑之后、删除之前） */
  rowButtons?: RowButton[]
  /** 挂到生成页的叠加层组件（如业务弹窗） */
  overlay?: Component
  /** 可查询字段；未配置则不显示搜索区 */
  searchFields?: SearchFieldConfig[]
  /** 主子表一对多 */
  detailTables?: DetailTableConfig[]
  /** 拦截默认增删改 */
  hooks?: CrudHooks
}
