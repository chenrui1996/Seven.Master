import type { PageExtension } from '../types'
import { ElMessage } from 'element-plus'
import i18n from '../../locales'

const t = i18n.global.t

const extension: PageExtension = {
  toolbarButtons: [],
  rowButtons: [
    {
      key: 'trigger',
      label: t('deviceComm.trigger'),
      permission: 'CommRule.Update',
      onClick: async (ctx) => {
        const id = Number(ctx.row.commRuleId ?? ctx.rowId)
        if (!id) return
        const res = await ctx.http.post<{ status: boolean; message?: string }>(
          `/api/DeviceComm/triggerRule/${id}`,
        )
        if (res.status) ElMessage.success(res.message || ctx.t('deviceComm.triggered'))
      },
    },
  ],
}

export default extension
