import { reactive } from 'vue'

/** Device 自定义处理弹窗状态（扩展按钮打开，非路由页） */
export type DeviceCustomMode = 'batch' | 'row'

export type DeviceCustomState = {
  visible: boolean
  mode: DeviceCustomMode
  rows: Record<string, unknown>[]
  /** 提交成功后刷新列表 */
  reload: (() => Promise<void>) | null
}

export const deviceCustomState = reactive<DeviceCustomState>({
  visible: false,
  mode: 'batch',
  rows: [],
  reload: null,
})

export function openDeviceCustomDialog(options: {
  mode: DeviceCustomMode
  rows: Record<string, unknown>[]
  reload?: () => Promise<void>
}) {
  deviceCustomState.mode = options.mode
  deviceCustomState.rows = options.rows.map((r) => ({ ...r, _remark: '' }))
  deviceCustomState.reload = options.reload ?? null
  deviceCustomState.visible = true
}

export function closeDeviceCustomDialog() {
  deviceCustomState.visible = false
  deviceCustomState.rows = []
  deviceCustomState.reload = null
}
