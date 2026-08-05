import type { PageExtension } from '../types'
import { ElMessage } from 'element-plus'

const extension: PageExtension = {
  toolbarButtons: [],
  rowButtons: [
    {
      key: 'trigger',
      label: '触发',
      permission: 'CommRule.Update',
      onClick: async (ctx) => {
        const id = Number(ctx.row.commRuleId ?? ctx.rowId)
        if (!id) return
        const res = await ctx.http.post<{ status: boolean; message?: string }>(
          `/api/DeviceComm/triggerRule/${id}`,
        )
        if (res.status) ElMessage.success(res.message || '已触发')
      },
    },
  ],
}

export default extension
