import { catalogEntry, parsePortRef } from '../../components/map/deviceCatalog'
import type { SimProject } from '../project/schema'

export type SimMap = SimProject['map']

export interface TopologyValidationError {
  code: string
  message: string
}

function validatePortRef(
  map: SimMap,
  ref: string,
  label: string,
  errors: TopologyValidationError[],
  codePrefix: string,
): void {
  const parsed = parsePortRef(ref)
  if (!parsed) {
    errors.push({ code: `${codePrefix}_FORMAT`, message: `${label} 端口格式无效: ${ref}` })
    return
  }
  const device = map.devices.find((d) => d.id === parsed.deviceId)
  if (!device) {
    errors.push({ code: `${codePrefix}_DEVICE`, message: `${label} 设备不存在: ${ref}` })
    return
  }
  const entry = catalogEntry(device.type)
  if (!entry) {
    errors.push({
      code: `${codePrefix}_TYPE`,
      message: `${label} 未知设备类型: ${device.code} (${device.type})`,
    })
    return
  }
  if (!entry.ports.some((p) => p.id === parsed.portId)) {
    errors.push({ code: `${codePrefix}_PORT`, message: `${label} 端口不存在: ${ref}` })
  }
}

/** Validate map topology before Deploy. Returns empty array when valid. */
export function validateTopology(map: SimMap): TopologyValidationError[] {
  const errors: TopologyValidationError[] = []

  if (!map.packId?.trim()) {
    errors.push({ code: 'PACK_ID_MISSING', message: '请选择 WCS 包 (packId)' })
  }

  if (!map.nodes.length) {
    errors.push({ code: 'NO_NODES', message: '至少需要一个库位/节点才能 Deploy' })
  }

  const seenCodes = new Map<string, string>()
  for (const node of map.nodes) {
    const c = node.code.trim()
    if (!c) {
      errors.push({ code: 'NODE_CODE_EMPTY', message: `节点 ${node.id.slice(0, 8)}… 缺少编码` })
      continue
    }
    if (seenCodes.has(c)) {
      errors.push({ code: 'DUPLICATE_NODE_CODE', message: `节点编码重复: ${c}` })
    } else {
      seenCodes.set(c, node.id)
    }
  }

  const nodeIds = new Set(map.nodes.map((n) => n.id))
  for (const edge of map.edges) {
    if (!nodeIds.has(edge.from)) {
      errors.push({ code: 'EDGE_FROM_MISSING', message: `边 ${edge.id.slice(0, 8)}… 的起点节点不存在` })
    }
    if (!nodeIds.has(edge.to)) {
      errors.push({ code: 'EDGE_TO_MISSING', message: `边 ${edge.id.slice(0, 8)}… 的终点节点不存在` })
    }
  }

  for (const conn of map.connections ?? []) {
    validatePortRef(map, conn.from, '连线起点', errors, `CONN_${conn.id}_FROM`)
    validatePortRef(map, conn.to, '连线终点', errors, `CONN_${conn.id}_TO`)
  }

  return errors
}
