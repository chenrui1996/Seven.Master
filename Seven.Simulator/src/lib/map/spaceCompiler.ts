import type { SimProject } from '../project/schema'

export type SimMap = SimProject['map']

/**
 * Thin stub for RCS device-space compilation.
 * Devices already carry canvas x/y; ensure defaults and pass through until a full compiler is ported.
 */
export function compileMapSpaces(map: SimMap): SimMap {
  const devices = map.devices.map((d) => ({
    ...d,
    x: d.x ?? 0,
    y: d.y ?? 0,
  }))
  return { ...map, devices }
}
