/** Shared node footprint for canvas draw + hit testing (top-left anchor). */
export const NODE_WIDTH = 72
export const NODE_HEIGHT = 32

export interface HitTestNode {
  id: string
  x: number
  y: number
}

export interface HitTestDevice extends HitTestNode {
  width: number
  height: number
}

export function nodeCenter(node: HitTestNode): { x: number; y: number } {
  return { x: node.x + NODE_WIDTH / 2, y: node.y + NODE_HEIGHT / 2 }
}

export function nodeBounds(node: HitTestNode): {
  left: number
  top: number
  right: number
  bottom: number
} {
  return {
    left: node.x,
    top: node.y,
    right: node.x + NODE_WIDTH,
    bottom: node.y + NODE_HEIGHT,
  }
}

export function deviceBounds(device: HitTestDevice): {
  left: number
  top: number
  right: number
  bottom: number
} {
  return {
    left: device.x,
    top: device.y,
    right: device.x + device.width,
    bottom: device.y + device.height,
  }
}

/** Returns the top-most node id at world coordinates, or null. */
export function hitTestNode(
  nodes: HitTestNode[],
  worldX: number,
  worldY: number,
): string | null {
  for (let i = nodes.length - 1; i >= 0; i--) {
    const n = nodes[i]
    const b = nodeBounds(n)
    if (worldX >= b.left && worldX <= b.right && worldY >= b.top && worldY <= b.bottom) {
      return n.id
    }
  }
  return null
}

/** Returns the top-most device id at world coordinates, or null. */
export function hitTestDevice(
  devices: HitTestDevice[],
  worldX: number,
  worldY: number,
): string | null {
  for (let i = devices.length - 1; i >= 0; i--) {
    const d = devices[i]
    const b = deviceBounds(d)
    if (worldX >= b.left && worldX <= b.right && worldY >= b.top && worldY <= b.bottom) {
      return d.id
    }
  }
  return null
}
