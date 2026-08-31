import type { PageActionContext, PageExtension } from '../types'
import { ActionIcons } from '../../constants/actionIcons'
import { ElMessage, ElMessageBox } from 'element-plus'

type ApiResult = { status: boolean; message?: string }

const extension: PageExtension = {
  searchFields: [
    { prop: 'taskNo', kind: 'string', operator: 'like' },
    { prop: 'materialCode', kind: 'string', operator: 'like' },
    { prop: 'status', kind: 'enum', operator: 'equal' },
    { prop: 'containerCode', kind: 'string', operator: 'like' },
  ],

  toolbarButtons: [
    {
      key: 'confirmPick',
      label: '确认拣选',
      icon: ActionIcons.Check,
      permission: 'WmsPickingTask.Update',
      onClick: async (ctx: PageActionContext) => {
        const row = ctx.selectedRows?.[0] as { id?: number } | undefined
        if (!row?.id) {
          ElMessage.warning(ctx.t('wmsOps.selectOne') || '请先选择一条拣选任务')
          return
        }
        const { value } = await ElMessageBox.prompt('拣货数量（空=账面量）', '确认拣选', {
          confirmButtonText: ctx.t('wmsOps.confirm') || '确定',
          cancelButtonText: ctx.t('wmsOps.cancel') || '取消',
          inputPattern: /^(\d+(\.\d+)?)?$/,
          inputErrorMessage: '数量无效',
        }).catch(() => ({ value: null as string | null }))
        if (value === null) return
        const pickQty = value === '' ? undefined : Number(value)
        const res = await ctx.http.post<ApiResult>('/api/WmsPickingTask/confirmPick', {
          pickingTaskId: row.id,
          pickQty,
        })
        if (res.status) {
          ElMessage.success(res.message || '拣选确认成功')
          await ctx.reload()
        } else if (res.message) {
          ElMessage.error(res.message)
        }
      },
    },
  ],

  rowButtons: [
    {
      key: 'confirmPickRow',
      label: '确认',
      permission: 'WmsPickingTask.Update',
      onClick: async (ctx: PageActionContext) => {
        const row = ctx.row as { id?: number }
        if (!row?.id) return
        const res = await ctx.http.post<ApiResult>('/api/WmsPickingTask/confirmPick', {
          pickingTaskId: row.id,
        })
        if (res.status) {
          ElMessage.success(res.message || '拣选确认成功')
          await ctx.reload()
        } else if (res.message) {
          ElMessage.error(res.message)
        }
      },
    },
  ],
}

export default extension
