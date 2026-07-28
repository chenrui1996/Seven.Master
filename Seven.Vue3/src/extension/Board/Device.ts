import type { PageExtension } from '../types'

/**
 * Device 业务扩展（重新生成 Vue 不会覆盖本文件）
 *
 * 使用图标时：
 *   import { ActionIcons } from '../../constants/actionIcons'
 *   icon: ActionIcons.sync
 *
 * 示例 — 提交后台：
 * {
 *   key: 'customAction',
 *   label: '自定义处理',
 *   icon: ActionIcons.sync,
 *   permission: 'Device.Update',
 *   type: 'warning',
 *   requireSelection: true,
 *   async onClick(ctx) {
 *     const res = await ctx.http.post('/api/Device/custom', ctx.selectedIds)
 *     if (res.status) await ctx.reload()
 *   },
 * }
 *
 * 示例 — 打开自定义页：
 * {
 *   key: 'openBatch',
 *   label: '批量页',
 *   icon: ActionIcons.viewAll,
 *   requireSelection: true,
 *   onClick(ctx) {
 *     ctx.router.push({
 *       path: '/DeviceBatch',
 *       query: { ids: ctx.selectedIds.join(',') },
 *     })
 *   },
 * }
 */
const extension: PageExtension = {
  toolbarButtons: [],
}

export default extension
