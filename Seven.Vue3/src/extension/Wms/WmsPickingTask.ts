import type { PageActionContext, PageExtension } from '../types'
import { ActionIcons } from '../../constants/actionIcons'
import { ElMessage, ElMessageBox } from 'element-plus'
import i18n from '../../locales'

const t = i18n.global.t

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
      label: t('wmsOps.outbound.confirmPick'),
      icon: ActionIcons.Check,
      permission: 'WmsPickingTask.Update',
      onClick: async (ctx: PageActionContext) => {
        const row = ctx.selectedRows?.[0] as { id?: number } | undefined
        if (!row?.id) {
          ElMessage.warning(ctx.t('wmsOps.selectOne'))
          return
        }
        const { value } = await ElMessageBox.prompt(
          ctx.t('wmsOps.confirmPickPrompt'),
          ctx.t('wmsOps.outbound.confirmPick'),
          {
            confirmButtonText: ctx.t('wmsOps.confirm'),
            cancelButtonText: ctx.t('wmsOps.cancel'),
            inputPattern: /^(\d+(\.\d+)?)?$/,
            inputErrorMessage: ctx.t('wmsOps.invalidQty'),
          },
        ).catch(() => ({ value: null as string | null }))
        if (value === null) return
        const pickQty = value === '' ? undefined : Number(value)
        const res = await ctx.http.post<ApiResult>('/api/WmsPickingTask/confirmPick', {
          pickingTaskId: row.id,
          pickQty,
        })
        if (res.status) {
          ElMessage.success(res.message || ctx.t('wmsOps.outbound.confirmPickSuccess'))
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
      label: t('wmsOps.confirmShort'),
      permission: 'WmsPickingTask.Update',
      onClick: async (ctx: PageActionContext) => {
        const row = ctx.row as { id?: number }
        if (!row?.id) return
        const res = await ctx.http.post<ApiResult>('/api/WmsPickingTask/confirmPick', {
          pickingTaskId: row.id,
        })
        if (res.status) {
          ElMessage.success(res.message || ctx.t('wmsOps.outbound.confirmPickSuccess'))
          await ctx.reload()
        } else if (res.message) {
          ElMessage.error(res.message)
        }
      },
    },
  ],
}

export default extension
