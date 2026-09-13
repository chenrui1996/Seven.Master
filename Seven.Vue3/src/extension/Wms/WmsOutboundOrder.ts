import type { PageActionContext, PageExtension } from '../types'
import { ActionIcons } from '../../constants/actionIcons'
import { ElMessage, ElMessageBox } from 'element-plus'
import i18n from '../../locales'

const t = i18n.global.t

const lineColumns = [
  { prop: 'lineNo', label: t('wmsOps.inbound.lineNo'), kind: 'number' as const, width: 70 },
  { prop: 'materialCode', label: t('wmsOps.material') },
  { prop: 'qty', label: t('wmsOps.qty'), kind: 'number' as const, width: 90 },
  { prop: 'completedQty', label: t('wmsOps.inbound.completed'), kind: 'number' as const, width: 90 },
  { prop: 'wcsPri', label: 'Pri', kind: 'number' as const, width: 70 },
  { prop: 'fromLocation', label: 'From', width: 110 },
  { prop: 'toLocation', label: 'To', width: 110 },
  { prop: 'containerCode', label: t('wmsOps.container'), width: 120 },
]

const pickColumns = [
  { prop: 'taskNo', label: t('generated.WmsPickingTask.taskNo'), width: 130 },
  { prop: 'materialCode', label: t('wmsOps.material') },
  { prop: 'bookQty', label: t('generated.WmsPickingTask.bookQty'), kind: 'number' as const, width: 80 },
  { prop: 'pickQty', label: t('generated.WmsPickingTask.pickQty'), kind: 'number' as const, width: 80 },
  { prop: 'fromLocation', label: 'From', width: 110 },
  { prop: 'toLocation', label: 'To', width: 110 },
  { prop: 'containerCode', label: t('wmsOps.container'), width: 120 },
  { prop: 'status', label: t('wmsOps.status'), kind: 'enum' as const, width: 90 },
]

type ApiResult = { status: boolean; message?: string }

async function postOrderAction(
  ctx: PageActionContext,
  url: string,
  data?: unknown,
  successKey = 'wmsOps.approveSuccess',
) {
  const res = await ctx.http.post<ApiResult>(url, data)
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
    { prop: 'wcsGroupNo', kind: 'string', operator: 'like' },
  ],

  detailTables: [
    {
      key: 'WmsOutboundOrderLine',
      title: t('wmsOps.outboundLines'),
      mode: 'below',
      apiRoute: 'WmsOutboundOrderLine',
      keyField: 'id',
      foreignKey: 'orderId',
      masterKey: 'id',
      columns: lineColumns,
      formFields: [
        { prop: 'lineNo', kind: 'number', defaultValue: 1 },
        { prop: 'materialCode', kind: 'string' },
        { prop: 'qty', kind: 'number', isDecimal: true, defaultValue: 1 },
        { prop: 'wcsPri', kind: 'number', defaultValue: 1 },
        { prop: 'fromLocation', kind: 'string' },
        { prop: 'toLocation', kind: 'string' },
        { prop: 'containerCode', kind: 'string' },
      ],
      children: [
        {
          key: 'WmsPickingTask',
          title: t('wmsOps.pickingTasks'),
          mode: 'below',
          apiRoute: 'WmsPickingTask',
          keyField: 'id',
          foreignKey: 'lineId',
          masterKey: 'id',
          columns: pickColumns,
          formFields: [
            { prop: 'taskNo', kind: 'string' },
            { prop: 'materialCode', kind: 'string' },
            { prop: 'bookQty', kind: 'number', isDecimal: true },
            { prop: 'pickQty', kind: 'number', isDecimal: true },
            { prop: 'fromLocation', kind: 'string' },
            { prop: 'toLocation', kind: 'string' },
            { prop: 'containerCode', kind: 'string' },
          ],
        },
      ],
    },
  ],

  toolbarButtons: [
    {
      key: 'approve',
      label: t('wmsOps.approve'),
      icon: ActionIcons.acknowledge,
      type: 'primary',
      requireSelection: true,
      permission: 'WmsOutboundOrder.Update',
      async onClick(ctx) {
        const id = ctx.selectedIds[0]
        if (!id) return
        await postOrderAction(ctx, `/api/WmsOutboundOrder/approve/${id}`, undefined, 'wmsOps.approveSuccess')
      },
    },
    {
      key: 'generatePicks',
      label: t('wmsOps.outbound.generatePicks'),
      icon: ActionIcons.addChild,
      type: 'warning',
      requireSelection: true,
      permission: 'WmsOutboundOrder.Update',
      async onClick(ctx) {
        const id = ctx.selectedIds[0]
        if (!id) return
        await postOrderAction(
          ctx,
          `/api/WmsOutboundOrder/generatePicks/${id}`,
          undefined,
          'wmsOps.outbound.generatePicksSuccess',
        )
      },
    },
    {
      key: 'ship',
      label: t('wmsOps.outbound.ship'),
      icon: ActionIcons.export,
      type: 'success',
      requireSelection: true,
      permission: 'WmsOutboundOrder.Update',
      async onClick(ctx) {
        const id = ctx.selectedIds[0]
        if (!id) return
        await postOrderAction(ctx, `/api/WmsOutboundOrder/ship/${id}`, undefined, 'wmsOps.outbound.shipSuccess')
      },
    },
    {
      key: 'confirmPick',
      label: t('wmsOps.outbound.confirmPick'),
      icon: ActionIcons.confirm,
      type: 'info',
      permission: 'WmsPickingTask.Update',
      async onClick(ctx) {
        try {
          const { value } = await ElMessageBox.prompt(
            ctx.t('wmsOps.outbound.pickingTaskId'),
            ctx.t('wmsOps.outbound.confirmPick'),
            {
              confirmButtonText: ctx.t('wmsOps.save'),
              cancelButtonText: ctx.t('wmsOps.cancel'),
              inputPattern: /^\d+$/,
              inputErrorMessage: ctx.t('wmsOps.outbound.pickingTaskId'),
            },
          )
          const pickingTaskId = Number(value)
          await postOrderAction(
            ctx,
            '/api/WmsPickingTask/confirmPick',
            { pickingTaskId, dispatchTransport: true },
            'wmsOps.outbound.confirmPickSuccess',
          )
        } catch {
          /* user cancelled */
        }
      },
    },
  ],
}

export default extension
