import type { PageActionContext, PageExtension } from '../types'
import { ActionIcons } from '../../constants/actionIcons'
import { ElMessage, ElMessageBox } from 'element-plus'
import i18n from '../../locales'

const t = i18n.global.t

const lineColumns = [
  { prop: 'lineNo', label: t('wmsOps.inbound.lineNo'), kind: 'number' as const, width: 70 },
  { prop: 'materialCode', label: t('wmsOps.material') },
  { prop: 'qty', label: t('wmsOps.inbound.planned'), kind: 'number' as const, width: 90 },
  { prop: 'completedQty', label: t('wmsOps.inbound.completed'), kind: 'number' as const, width: 90 },
  { prop: 'containerCode', label: t('wmsOps.container'), width: 120 },
]

const detailColumns = [
  { prop: 'detailNo', label: '#', kind: 'number' as const, width: 60 },
  { prop: 'materialCode', label: t('wmsOps.material') },
  { prop: 'qty', label: t('wmsOps.qty'), kind: 'number' as const, width: 80 },
  { prop: 'containerCode', label: t('wmsOps.container'), width: 120 },
  { prop: 'receiveLocationCode', label: t('wmsOps.inbound.receiveLoc'), width: 120 },
  { prop: 'targetLocationCode', label: t('wmsOps.inbound.targetLoc'), width: 120 },
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
  ],

  detailTables: [
    {
      key: 'WmsInboundOrderLine',
      title: t('wmsOps.inboundLines'),
      mode: 'below',
      apiRoute: 'WmsInboundOrderLine',
      keyField: 'id',
      foreignKey: 'orderId',
      masterKey: 'id',
      columns: lineColumns,
      formFields: [
        { prop: 'lineNo', kind: 'number', defaultValue: 1 },
        { prop: 'materialCode', kind: 'string' },
        { prop: 'qty', kind: 'number', isDecimal: true, defaultValue: 1 },
        { prop: 'containerCode', kind: 'string' },
      ],
      children: [
        {
          key: 'WmsInboundDetail',
          title: t('wmsOps.inbound.palletDetails'),
          mode: 'below',
          apiRoute: 'WmsInboundDetail',
          keyField: 'id',
          foreignKey: 'lineId',
          masterKey: 'id',
          columns: detailColumns,
          formFields: [
            { prop: 'detailNo', kind: 'number', defaultValue: 1 },
            { prop: 'materialCode', kind: 'string' },
            { prop: 'qty', kind: 'number', isDecimal: true, defaultValue: 1 },
            { prop: 'containerCode', kind: 'string' },
            { prop: 'receiveLocationCode', kind: 'string' },
            { prop: 'targetLocationCode', kind: 'string' },
            { prop: 'packId', kind: 'string', defaultValue: 'stacker' },
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
      permission: 'WmsInboundOrder.Update',
      async onClick(ctx) {
        const id = ctx.selectedIds[0]
        if (!id) return
        await postOrderAction(ctx, `/api/WmsInboundOrder/approve/${id}`, undefined, 'wmsOps.approveSuccess')
      },
    },
    {
      key: 'receive',
      label: t('wmsOps.inbound.receive'),
      icon: ActionIcons.confirm,
      type: 'success',
      requireSelection: true,
      permission: 'WmsInboundOrder.Update',
      async onClick(ctx) {
        const id = ctx.selectedIds[0]
        if (!id) return
        await postOrderAction(ctx, `/api/WmsInboundOrder/receive/${id}`, {}, 'wmsOps.inbound.receiveSuccess')
      },
    },
    {
      key: 'buildPallet',
      label: t('wmsOps.inbound.pallet'),
      icon: ActionIcons.add,
      type: 'warning',
      requireSelection: true,
      permission: 'WmsInboundOrder.Update',
      async onClick(ctx) {
        const id = ctx.selectedIds[0]
        if (!id) return
        try {
          const { value: containerCode } = await ElMessageBox.prompt(
            ctx.t('wmsOps.container'),
            ctx.t('wmsOps.inbound.pallet'),
            {
              confirmButtonText: ctx.t('wmsOps.create'),
              cancelButtonText: ctx.t('wmsOps.cancel'),
              inputPattern: /\S+/,
              inputErrorMessage: ctx.t('wmsOps.container'),
            },
          )
          const { value: receiveLocationCode } = await ElMessageBox.prompt(
            ctx.t('wmsOps.inbound.receiveLoc'),
            ctx.t('wmsOps.inbound.pallet'),
            {
              confirmButtonText: ctx.t('wmsOps.inbound.submitPallet'),
              cancelButtonText: ctx.t('wmsOps.cancel'),
              inputPattern: /\S+/,
              inputErrorMessage: ctx.t('wmsOps.inbound.receiveLoc'),
            },
          )
          await postOrderAction(
            ctx,
            `/api/WmsInboundOrder/buildPallet/${id}`,
            {
              lineNo: 1,
              qty: 1,
              containerCode,
              receiveLocationCode,
              packId: 'stacker',
              allocateTarget: true,
              height: 0,
              weight: 0,
            },
            'wmsOps.inbound.palletSuccess',
          )
        } catch {
          /* user cancelled */
        }
      },
    },
  ],
}

export default extension
