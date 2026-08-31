/**
 * 业务扩展样板：仓内调拨
 * 流程：新建头 → 明细补 From/To → 审核(Book) → 完成(ConfirmPick+Receive)
 */
import type { PageActionContext, PageExtension } from '../types'
import { ActionIcons } from '../../constants/actionIcons'
import { ElMessage } from 'element-plus'
import i18n from '../../locales'

const t = i18n.global.t

const lineColumns = [
  { prop: 'lineNo', label: t('generated.TransferOrderLine.lineNo'), kind: 'number' as const, width: 70 },
  { prop: 'materialCode', label: t('generated.TransferOrderLine.materialCode') },
  { prop: 'qty', label: t('generated.TransferOrderLine.qty'), kind: 'number' as const, width: 90 },
  { prop: 'completedQty', label: t('generated.TransferOrderLine.completedQty'), kind: 'number' as const, width: 90 },
  { prop: 'fromLocation', label: t('generated.TransferOrderLine.fromLocation'), width: 120 },
  { prop: 'toLocation', label: t('generated.TransferOrderLine.toLocation'), width: 120 },
  { prop: 'containerCode', label: t('generated.TransferOrderLine.containerCode'), width: 120 },
]

type ApiResult = { status: boolean; message?: string }

async function postOrderAction(ctx: PageActionContext, url: string, successKey: string) {
  const res = await ctx.http.post<ApiResult>(url)
  if (res.status) {
    ElMessage.success(res.message || ctx.t(successKey))
    await ctx.reload()
  } else if (res.message) {
    ElMessage.error(res.message)
  }
}

const extension: PageExtension = {
  searchFields: [
    { prop: 'orderNo', kind: 'string', operator: 'like' },
    { prop: 'status', kind: 'enum', operator: 'equal' },
  ],

  detailTables: [
    {
      key: 'TransferOrderLine',
      title: t('bizOps.linesTitle'),
      mode: 'below',
      apiRoute: 'TransferOrderLine',
      keyField: 'id',
      foreignKey: 'orderId',
      masterKey: 'id',
      columns: lineColumns,
      formFields: [
        { prop: 'lineNo', kind: 'number', defaultValue: 1 },
        { prop: 'materialCode', kind: 'string' },
        { prop: 'qty', kind: 'number', isDecimal: true, defaultValue: 1 },
        { prop: 'fromLocation', kind: 'string' },
        { prop: 'toLocation', kind: 'string' },
        { prop: 'containerCode', kind: 'string' },
      ],
    },
  ],

  toolbarButtons: [
    {
      key: 'approve',
      label: t('bizOps.approve'),
      icon: ActionIcons.acknowledge,
      type: 'primary',
      requireSelection: true,
      permission: 'TransferOrder.Update',
      async onClick(ctx) {
        const id = ctx.selectedIds[0]
        if (!id) return
        await postOrderAction(ctx, `/api/TransferOrder/approve/${id}`, 'bizOps.approveSuccess')
      },
    },
    {
      key: 'complete',
      label: t('bizOps.complete'),
      icon: ActionIcons.save,
      type: 'success',
      requireSelection: true,
      permission: 'TransferOrder.Update',
      async onClick(ctx) {
        const id = ctx.selectedIds[0]
        if (!id) return
        await postOrderAction(ctx, `/api/TransferOrder/complete/${id}`, 'bizOps.completeSuccess')
      },
    },
  ],
}

export default extension
