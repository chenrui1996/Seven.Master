import { reactive } from 'vue'

export interface MapCameraState {
  offsetX: number
  offsetY: number
  scale: number
}

const MIN_SCALE = 0.25
const MAX_SCALE = 4

export function useMapCamera(initial?: Partial<MapCameraState>) {
  const camera = reactive<MapCameraState>({
    offsetX: initial?.offsetX ?? 0,
    offsetY: initial?.offsetY ?? 0,
    scale: initial?.scale ?? 1,
  })

  function pan(dx: number, dy: number) {
    camera.offsetX += dx
    camera.offsetY += dy
  }

  function zoomAt(deltaY: number, centerX: number, centerY: number) {
    const factor = deltaY < 0 ? 1.12 : 0.88
    const nextScale = Math.min(MAX_SCALE, Math.max(MIN_SCALE, camera.scale * factor))
    const ratio = nextScale / camera.scale
    camera.offsetX = centerX - (centerX - camera.offsetX) * ratio
    camera.offsetY = centerY - (centerY - camera.offsetY) * ratio
    camera.scale = nextScale
  }

  function screenToWorld(sx: number, sy: number): { x: number; y: number } {
    return {
      x: (sx - camera.offsetX) / camera.scale,
      y: (sy - camera.offsetY) / camera.scale,
    }
  }

  function worldToScreen(wx: number, wy: number): { x: number; y: number } {
    return {
      x: wx * camera.scale + camera.offsetX,
      y: wy * camera.scale + camera.offsetY,
    }
  }

  return { camera, pan, zoomAt, screenToWorld, worldToScreen }
}
