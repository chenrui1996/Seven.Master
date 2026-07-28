import type { PageExtension } from '../types'
import { ActionIcons } from '../../constants/actionIcons'
import { ElMessage } from 'element-plus'
import DeviceCustomDialog from '../../components/DeviceCustomDialog.vue'
import { openDeviceCustomDialog } from './deviceCustomShared'

/**
 * Device 业务扩展（重新生成 Vue 不会覆盖本文件）
 *
 * - 自定义弹窗：BatchCustom / RowCustom
 * - 查询区：searchFields
 * - 主子表：below + dialog
 * - hooks：拦截默认增删改
 */

const extension: PageExtension = {
  overlay: DeviceCustomDialog,

  searchFields: [
    { prop: 'deviceName', kind: 'string', operator: 'like' },
    { prop: 'deviceCode', kind: 'string', operator: 'like' },
    {
      prop: 'status',
      kind: 'enum',
      operator: 'equal',
      options: [
        { value: 0, label: '离线' },
        { value: 1, label: '在线' },
        { value: 2, label: '故障' },
        { value: 3, label: '维护中' },
      ],
    },
  ],

  detailTables: [
    {
      key: 'runtimeLogs',
      title: '运行记录',
      mode: 'below',
      columns: [
        { prop: 'time', label: '时间', kind: 'string', width: 180 },
        { prop: 'action', label: '动作', kind: 'string' },
        { prop: 'operator', label: '操作人', kind: 'string', width: 120 },
      ],
      async load(master) {
        const id = Number(master.deviceId ?? 0)
        // Demo：无真实子表 API，按主表行模拟明细
        return [
          { time: '2026-07-28 09:00:00', action: '设备上线', operator: '系统', deviceId: id },
          { time: '2026-07-28 10:30:00', action: `心跳正常（${master.deviceCode || id}）`, operator: '系统', deviceId: id },
          { time: '2026-07-28 14:00:00', action: '状态同步', operator: '管理员', deviceId: id },
        ]
      },
    },
    {
      key: 'spareParts',
      title: '备件清单',
      mode: 'dialog',
      buttonLabel: '备件清单',
      columns: [
        { prop: 'code', label: '物料编码', width: 140 },
        { prop: 'name', label: '物料名称' },
        { prop: 'qty', label: '数量', kind: 'number', width: 90 },
        { prop: 'unit', label: '单位', width: 80 },
      ],
      async load(master) {
        const name = String(master.deviceName ?? '设备')
        return [
          { code: 'SP-001', name: `${name}-轴承`, qty: 2, unit: '个' },
          { code: 'SP-002', name: `${name}-皮带`, qty: 1, unit: '条' },
        ]
      },
    },
  ],

  hooks: {
    async beforeOpenForm({ mode }) {
      if (mode === 'add') {
        // 返回 false 可取消打开；此处仅演示放行
      }
      return true
    },
    async beforeSave({ mode, form }) {
      const name = String(form.deviceName ?? '').trim()
      if (!name) {
        ElMessage.warning('设备名称不能为空（hooks.beforeSave）')
        return false
      }
      form.deviceName = name
      if (mode === 'add' && !form.deviceCode) {
        form.deviceCode = `DEV-${Date.now().toString().slice(-6)}`
        ElMessage.info(`已自动生成编码 ${form.deviceCode}`)
      }
      return true
    },
    async afterSave() {
      // 可在此刷新关联数据、打点等
    },
    async beforeDelete({ ids }) {
      if (ids.length > 5) {
        ElMessage.warning('单次最多删除 5 条（hooks.beforeDelete Demo）')
        return false
      }
      return true
    },
  },

  toolbarButtons: [
    {
      key: 'batchCustom',
      label: '批量处理',
      icon: ActionIcons.viewAll,
      type: 'warning',
      permission: 'Device.BatchCustom',
      requireSelection: true,
      onClick(ctx) {
        openDeviceCustomDialog({
          mode: 'batch',
          rows: ctx.selectedRows,
          reload: ctx.reload,
        })
      },
    },
  ],
  rowButtons: [
    {
      key: 'rowCustom',
      label: '处理',
      icon: ActionIcons.sync,
      type: 'warning',
      permission: 'Device.RowCustom',
      onClick(ctx) {
        openDeviceCustomDialog({
          mode: 'row',
          rows: [ctx.row],
          reload: ctx.reload,
        })
      },
    },
  ],
}

export default extension
