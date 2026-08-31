import type { SimMapDevice } from '@/lib/project/schema'

export type DeviceCatalogType = 'SRM' | 'CoordPoint' | 'Conveyor' | 'RequestPoint'

export interface DevicePortDef {
  id: string
  label: string
  /** Normalized offset from device center (-1..1). */
  nx: number
  ny: number
}

export interface DeviceCatalogEntry {
  type: DeviceCatalogType
  label: string
  width: number
  height: number
  fill: string
  stroke: string
  shape: 'rect' | 'diamond' | 'circle'
  ports: DevicePortDef[]
}

export const DEVICE_CATALOG: Record<DeviceCatalogType, DeviceCatalogEntry> = {
  SRM: {
    type: 'SRM',
    label: '堆垛机',
    width: 64,
    height: 48,
    fill: '#2d6a4f',
    stroke: '#40916c',
    shape: 'rect',
    ports: [
      { id: 'fork', label: '货叉', nx: 0, ny: -0.5 },
      { id: 'aisle', label: '巷道', nx: 0, ny: 0.5 },
    ],
  },
  CoordPoint: {
    type: 'CoordPoint',
    label: '坐标点',
    width: 40,
    height: 40,
    fill: '#7b2cbf',
    stroke: '#9d4edd',
    shape: 'diamond',
    ports: [{ id: 'io', label: 'IO', nx: 0, ny: 0 }],
  },
  Conveyor: {
    type: 'Conveyor',
    label: '输送线',
    width: 80,
    height: 32,
    fill: '#bc6c25',
    stroke: '#dda15e',
    shape: 'rect',
    ports: [
      { id: 'in', label: '入口', nx: -0.5, ny: 0 },
      { id: 'out', label: '出口', nx: 0.5, ny: 0 },
    ],
  },
  RequestPoint: {
    type: 'RequestPoint',
    label: '申请点',
    width: 48,
    height: 48,
    fill: '#c9184a',
    stroke: '#ff4d6d',
    shape: 'circle',
    ports: [{ id: 'loc', label: '库位', nx: 0, ny: 0 }],
  },
}

export const DEVICE_CATALOG_LIST: DeviceCatalogEntry[] = Object.values(DEVICE_CATALOG)

export function catalogEntry(type: string): DeviceCatalogEntry | undefined {
  return DEVICE_CATALOG[type as DeviceCatalogType]
}

export function nextDeviceCode(type: DeviceCatalogType, devices: SimMapDevice[]): string {
  const prefix: Record<DeviceCatalogType, string> = {
    SRM: 'SRM',
    CoordPoint: 'CP',
    Conveyor: 'CV',
    RequestPoint: 'RP',
  }
  let n = devices.filter((d) => d.type === type).length + 1
  let code = `${prefix[type]}${String(n).padStart(2, '0')}`
  while (devices.some((d) => d.code === code)) {
    n++
    code = `${prefix[type]}${String(n).padStart(2, '0')}`
  }
  return code
}

export function parsePortRef(ref: string): { deviceId: string; portId: string } | null {
  const dot = ref.lastIndexOf('.')
  if (dot <= 0) return null
  return { deviceId: ref.slice(0, dot), portId: ref.slice(dot + 1) }
}

export function formatPortRef(deviceId: string, portId: string): string {
  return `${deviceId}.${portId}`
}

export function deviceCenter(device: SimMapDevice): { x: number; y: number } {
  const entry = catalogEntry(device.type)
  const w = entry?.width ?? 48
  const h = entry?.height ?? 48
  return { x: device.x + w / 2, y: device.y + h / 2 }
}

export function portWorldPosition(
  device: SimMapDevice,
  portId: string,
): { x: number; y: number } | null {
  const entry = catalogEntry(device.type)
  if (!entry) return null
  const port = entry.ports.find((p) => p.id === portId)
  if (!port) return null
  const center = deviceCenter(device)
  return {
    x: center.x + port.nx * (entry.width / 2),
    y: center.y + port.ny * (entry.height / 2),
  }
}

export function portLabel(device: SimMapDevice, portId: string): string {
  const entry = catalogEntry(device.type)
  const port = entry?.ports.find((p) => p.id === portId)
  return port ? `${device.code}.${port.label}` : `${device.code}.${portId}`
}
