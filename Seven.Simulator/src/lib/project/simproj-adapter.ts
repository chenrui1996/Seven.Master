import { mergeSimProject, type SimProject } from './schema'

/** RCS / simulation-spa `.simproj.json` subset (Phase IV). */
export interface SimProjSource {
  version?: number
  meta?: {
    name?: string
    features?: SimProject['meta']['features']
    runtimeMode?: SimProject['meta']['runtimeMode']
    simCommsMode?: SimProject['meta']['simCommsMode']
  }
  map?: {
    packId?: string
    nodes?: Array<{ id?: string; code?: string; x?: number; y?: number; kind?: string }>
    edges?: Array<{ id?: string; from?: string; to?: string }>
    devices?: Array<{ id?: string; code?: string; type?: string; x?: number; y?: number }>
    connections?: Array<{ id?: string; from?: string; to?: string }>
    requestPoints?: Array<{ code?: string; mappedLocationCode?: string }>
  }
  scada?: SimProject['scada']
  promote?: SimProject['promote']
}

export interface SimProjAdaptResult {
  project: SimProject
  warnings: string[]
}

const SUPPORTED_DEVICE_TYPES = new Set(['SRM', 'CoordPoint', 'Conveyor', 'RequestPoint'])

/**
 * Map `.simproj.json` core fields → `.sevenproj.json`.
 * Unsupported device types are listed in `warnings` but do not block import.
 */
export function adaptSimProj(source: SimProjSource): SimProjAdaptResult {
  const warnings: string[] = []
  const map = source.map ?? {}

  const nodes = (map.nodes ?? []).map((n, i) => ({
    id: n.id?.trim() || `node-${i + 1}`,
    code: n.code?.trim() || `N${i + 1}`,
    x: n.x ?? 0,
    y: n.y ?? 0,
  }))

  const edges = (map.edges ?? []).map((e, i) => ({
    id: e.id?.trim() || `edge-${i + 1}`,
    from: e.from?.trim() ?? '',
    to: e.to?.trim() ?? '',
  }))

  const devices = (map.devices ?? []).map((d, i) => {
    const type = (d.type ?? 'Unknown').trim()
    if (!SUPPORTED_DEVICE_TYPES.has(type)) {
      warnings.push(`Unsupported device type "${type}" (${d.code ?? `device-${i + 1}`}); kept as-is`)
    }
    return {
      id: d.id?.trim() || `dev-${i + 1}`,
      code: d.code?.trim() || type + String(i + 1).padStart(2, '0'),
      type,
      x: d.x ?? 0,
      y: d.y ?? 0,
    }
  })

  const connections = (map.connections ?? []).map((c, i) => ({
    id: c.id?.trim() || `conn-${i + 1}`,
    from: c.from?.trim() ?? '',
    to: c.to?.trim() ?? '',
  }))

  const requestPoints = (map.requestPoints ?? []).map((rp) => ({
    code: rp.code?.trim() ?? '',
    mappedLocationCode: rp.mappedLocationCode?.trim() ?? '',
  }))

  if (!source.meta?.name) warnings.push('Missing meta.name; using default project name')
  if (nodes.length === 0 && devices.length === 0) {
    warnings.push('No nodes or devices found in map')
  }

  return {
    project: mergeSimProject({
      version: source.version ?? 1,
      meta: {
        name: source.meta?.name ?? 'ImportedSimProj',
        ...(source.meta?.features !== undefined ? { features: source.meta.features } : {}),
        ...(source.meta?.runtimeMode !== undefined ? { runtimeMode: source.meta.runtimeMode } : {}),
        ...(source.meta?.simCommsMode !== undefined ? { simCommsMode: source.meta.simCommsMode } : {}),
      },
      map: {
        packId: map.packId ?? 'stacker',
        nodes,
        edges,
        devices,
        connections,
        requestPoints,
      },
      scada: source.scada,
      promote: source.promote,
    } as Partial<SimProject>),
    warnings,
  }
}

/** Parse JSON text from a `.simproj.json` file. */
export function adaptSimProjJson(text: string): SimProjAdaptResult {
  const parsed = JSON.parse(text) as SimProjSource
  return adaptSimProj(parsed)
}
