import type { PageExtension } from '../types'
import { ActionIcons } from '../../constants/actionIcons'
import { ElMessage } from 'element-plus'
import DeviceCustomDialog from '../../components/DeviceCustomDialog.vue'
import { openDeviceCustomDialog } from './deviceCustomShared'

/**
 * Device 业务扩展（重新生成 Vue 不会覆盖本文件）
 *
 * - 自定义弹窗：BatchCustom / RowCustom（见 doc/11）
 * - 查询 / hooks / 主子表（见 doc/12）
 * - 主子表 Demo：
 *   - below：SubDevice 真实 CRUD（apiRoute）
 *   - dialog：同一子表弹窗入口
 *   - page：新标签打开 SubDevice 标准页（?deviceId=）
 */

const subDeviceColumns = [
  { prop: 'subDeviceName', label: '子设备名称' },
  { prop: 'subDeviceCode', label: '编码', width: 120 },
  { prop: 'status', label: '状态', kind: 'enum' as const, width: 90 },
  { prop: 'remark', label: '备注' },
]

const subDeviceFormFields = [
  { prop: 'subDeviceName', kind: 'string' as const },
  { prop: 'subDeviceCode', kind: 'string' as const },
  { prop: 'status', kind: 'enum' as const },
  { prop: 'remark', kind: 'string' as const },
]

const subDeviceSearchFields = [
  { prop: 'subDeviceName', kind: 'string' as const, operator: 'like' as const, label: '子设备名称' },
]

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
      key: 'SubDevice',
      title: '子设备',
      mode: 'below',
      apiRoute: 'SubDevice',
      keyField: 'subDeviceId',
      foreignKey: 'deviceId',
      masterKey: 'deviceId',
      columns: subDeviceColumns,
      formFields: subDeviceFormFields,
      searchFields: subDeviceSearchFields,
    },
    {
      key: 'SubDeviceDialog',
      title: '子设备',
      mode: 'dialog',
      buttonLabel: '子设备',
      apiRoute: 'SubDevice',
      keyField: 'subDeviceId',
      foreignKey: 'deviceId',
      masterKey: 'deviceId',
      columns: subDeviceColumns,
      formFields: subDeviceFormFields,
      searchFields: subDeviceSearchFields,
    },
    {
      key: 'SubDevicePage',
      title: '子设备',
      mode: 'page',
      buttonLabel: '子设备页',
      apiRoute: 'SubDevice',
      keyField: 'subDeviceId',
      foreignKey: 'deviceId',
      masterKey: 'deviceId',
      columns: subDeviceColumns,
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
