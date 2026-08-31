export type SimCommsMode = 'Trigger' | 'Gateway'

export interface SimFeatures {
  wms: boolean
  orchestrationBus: boolean
  hotStore: boolean
  deviceComm: boolean
  simulator: boolean
  wcsPacks: {
    stacker: boolean
    fourWay: boolean
    boxSort: boolean
  }
}

export interface SimMapNode {
  id: string
  code: string
  x: number
  y: number
}

export interface SimMapEdge {
  id: string
  from: string
  to: string
}

export interface SimMapDevice {
  id: string
  code: string
  type: string
  x: number
  y: number
}

/**
 * Device port link stored as `"deviceId.portId"` on both ends.
 *
 * **Deploy compile rule (Phase II, client-side before POST deploy):**
 * Backend does not read `connections` yet; MapEditor calls `compileMapForDeploy`
 * to merge synthesized rows into `edges` / `requestPoints` for the deploy payload.
 * - Connection involving a RequestPoint device → `requestPoint` with
 *   `code = device.code` and `mappedLocationCode` = nearest location node to the other endpoint.
 * - Other device-device connections → `edge` between nearest location nodes to each endpoint
 *   (skipped when both resolve to the same node). Manual edges/requestPoints are preserved.
 */
export interface SimMapConnection {
  id: string
  from: string
  to: string
}

export interface SimMapRequestPoint {
  code: string
  mappedLocationCode: string
}

export interface SimScadaView {
  code: string
  name: string
  width: number
  height: number
}

export interface SimPromoteDevice {
  code: string
  host: string
  port: number
  protocol: string
}

export interface SimProject {
  version: number
  meta: {
    name: string
    features: SimFeatures
    runtimeMode: 'Simulation' | 'Production'
    simCommsMode: SimCommsMode
  }
  map: {
    packId: string
    nodes: SimMapNode[]
    edges: SimMapEdge[]
    devices: SimMapDevice[]
    connections: SimMapConnection[]
    requestPoints: SimMapRequestPoint[]
  }
  scada: {
    views: SimScadaView[]
  }
  promote: {
    devices: SimPromoteDevice[]
  }
}

export const defaultSimFeatures = (): SimFeatures => ({
  wms: true,
  orchestrationBus: true,
  hotStore: false,
  deviceComm: false,
  simulator: true,
  wcsPacks: { stacker: true, fourWay: false, boxSort: false },
})

export const defaultSimProject = (): SimProject => ({
  version: 1,
  meta: {
    name: 'Demo',
    features: defaultSimFeatures(),
    runtimeMode: 'Simulation',
    simCommsMode: 'Trigger',
  },
  map: { packId: 'stacker', nodes: [], edges: [], devices: [], connections: [], requestPoints: [] },
  scada: { views: [] },
  promote: { devices: [] },
})

/** Reject loopback hosts for Promote (aligned with backend). */
export function isLoopbackHost(host: string): boolean {
  const h = host.trim().toLowerCase()
  return h === '127.0.0.1' || h === 'localhost' || h === '::1'
}

/** Merge imported or persisted partial projects with defaults. */
export function mergeSimProject(parsed: Partial<SimProject>): SimProject {
  const defaults = defaultSimProject()
  return {
    version: parsed.version ?? defaults.version,
    meta: {
      ...defaults.meta,
      ...parsed.meta,
      name: parsed.meta?.name ?? defaults.meta.name,
      runtimeMode: parsed.meta?.runtimeMode ?? defaults.meta.runtimeMode,
      simCommsMode: parsed.meta?.simCommsMode ?? defaults.meta.simCommsMode,
      features: {
        ...defaults.meta.features,
        ...parsed.meta?.features,
        wcsPacks: {
          ...defaults.meta.features.wcsPacks,
          ...parsed.meta?.features?.wcsPacks,
        },
      },
    },
    map: {
      packId: parsed.map?.packId ?? defaults.map.packId,
      nodes: parsed.map?.nodes ?? defaults.map.nodes,
      edges: parsed.map?.edges ?? defaults.map.edges,
      devices: parsed.map?.devices ?? defaults.map.devices,
      connections: parsed.map?.connections ?? defaults.map.connections,
      requestPoints: parsed.map?.requestPoints ?? defaults.map.requestPoints,
    },
    scada: {
      views: parsed.scada?.views ?? defaults.scada.views,
    },
    promote: {
      devices: parsed.promote?.devices ?? defaults.promote.devices,
    },
  }
}
