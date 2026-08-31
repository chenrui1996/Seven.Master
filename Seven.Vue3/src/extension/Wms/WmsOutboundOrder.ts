import type { PageActionContext, PageExtension } from '../types'
import { ActionIcons } from '../../constants/actionIcons'
import { ElMessage, ElMessageBox } from 'element-plus'

const lineColumns = [
  { prop: 'lineNo', label: '行号', kind: 'number' as const, width: 70 },
  { prop: 'materialCode', label: '物料' },
  { prop: 'qty', label: '数量', kind: 'number' as const, width: 90 },
  { prop: 'completedQty', label: '完成', kind: 'number' as const, width: 90 },
  { prop: 'wcsPri', label: 'Pri', kind: 'number' as const, width: 70 },
  { prop: 'fromLocation', label: 'From', width: 110 },
  { prop: 'toLocation', label: 'To', width: 110 },
  { prop: 'containerCode', label: '容器', width: 120 },
]

const pickColumns = [
  { prop: 'taskNo', label: '任务号', width: 130 },
  { prop: 'materialCode', label: '物料' },
  { prop: 'bookQty', label: '账面', kind: 'number' as const, width: 80 },
  { prop: 'pickQty', label: '拣选', kind: 'number' as const, width: 80 },
  { prop: 'fromLocation', label: 'From', width: 110 },
  { prop: 'toLocation', label: 'To', width: 110 },
  { prop: 'containerCode', label: '容器', width: 120 },
  { prop: 'status', label: '状态', kind: 'enum' as const, width: 90 },
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
      title: '出库行',
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
          title: '拣选任务',
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
      label: '审核',
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
      label: '生成拣选',
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
      label: '发运',
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
      label: '确认拣选',
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
