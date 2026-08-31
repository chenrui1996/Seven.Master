import { NODE_HEIGHT, NODE_WIDTH } from '@/components/map/hitTest'
import { catalogEntry, deviceCenter, parsePortRef } from '@/components/map/deviceCatalog'
import type { SimMapConnection, SimMapEdge, SimMapNode, SimMapRequestPoint, SimProject } from './schema'

type SimMap = SimProject['map']

function nodeCenter(node: SimMapNode): { x: number; y: number } {
  return { x: node.x + NODE_WIDTH / 2, y: node.y + NODE_HEIGHT / 2 }
}

function dist(ax: number, ay: number, bx: number, by: number): number {
  const dx = ax - bx
  const dy = ay - by
  return dx * dx + dy * dy
}

function nearestNode(nodes: SimMapNode[], wx: number, wy: number): SimMapNode | null {
  if (!nodes.length) return null
  let best = nodes[0]
  let bestD = dist(wx, wy, nodeCenter(best).x, nodeCenter(best).y)
  for (let i = 1; i < nodes.length; i++) {
    const n = nodes[i]
    const d = dist(wx, wy, nodeCenter(n).x, nodeCenter(n).y)
    if (d < bestD) {
      best = n
      bestD = d
    }
  }
  return best
}

function deviceById(map: SimMap, id: string) {
  return map.devices.find((d) => d.id === id)
}

function endpointWorld(map: SimMap, portRef: string): { x: number; y: number } | null {
  const parsed = parsePortRef(portRef)
  if (!parsed) return null
  const device = deviceById(map, parsed.deviceId)
  if (!device) return null
  const entry = catalogEntry(device.type)
  if (!entry) return deviceCenter(device)
  const port = entry.ports.find((p) => p.id === parsed.portId)
  if (!port) return deviceCenter(device)
  const center = deviceCenter(device)
  return {
    x: center.x + port.nx * (entry.width / 2),
    y: center.y + port.ny * (entry.height / 2),
  }
}

function edgeKey(from: string, to: string): string {
  return from < to ? `${from}|${to}` : `${to}|${from}`
}

/**
 * Client-side deploy prep: merge synthesized edges/requestPoints from `connections`.
 * See schema.ts SimMapConnection comment for the Phase II rule summary.
 */
export function compileMapForDeploy(map: SimMap): SimMap {
  const nodes = map.nodes
  const edges: SimMapEdge[] = [...map.edges]
  const requestPoints: SimMapRequestPoint[] = [...map.requestPoints]
  const connections: SimMapConnection[] = map.connections ?? []

  const edgeKeys = new Set(edges.map((e) => edgeKey(e.from, e.to)))
  const rpCodes = new Set(requestPoints.map((rp) => rp.code))

  for (const conn of connections) {
    const fromParsed = parsePortRef(conn.from)
    const toParsed = parsePortRef(conn.to)
    if (!fromParsed || !toParsed) continue

    const fromDev = deviceById(map, fromParsed.deviceId)
    const toDev = deviceById(map, toParsed.deviceId)
    if (!fromDev || !toDev) continue

    const involvesRequestPoint =
      fromDev.type === 'RequestPoint' || toDev.type === 'RequestPoint'

    if (involvesRequestPoint) {
      const rpDev = fromDev.type === 'RequestPoint' ? fromDev : toDev
      const otherDev = rpDev === fromDev ? toDev : fromDev
      const otherPt = endpointWorld(map, rpDev === fromDev ? conn.to : conn.from) ?? deviceCenter(otherDev)
      const mapped = nearestNode(nodes, otherPt.x, otherPt.y)
      if (mapped && !rpCodes.has(rpDev.code)) {
        requestPoints.push({ code: rpDev.code, mappedLocationCode: mapped.code })
        rpCodes.add(rpDev.code)
      }
      continue
    }

    const fromPt = endpointWorld(map, conn.from) ?? deviceCenter(fromDev)
    const toPt = endpointWorld(map, conn.to) ?? deviceCenter(toDev)
    const fromNode = nearestNode(nodes, fromPt.x, fromPt.y)
    const toNode = nearestNode(nodes, toPt.x, toPt.y)
    if (!fromNode || !toNode || fromNode.id === toNode.id) continue
    const key = edgeKey(fromNode.id, toNode.id)
    if (edgeKeys.has(key)) continue
    edges.push({ id: crypto.randomUUID(), from: fromNode.id, to: toNode.id })
    edgeKeys.add(key)
  }

  return { ...map, edges, requestPoints }
}
