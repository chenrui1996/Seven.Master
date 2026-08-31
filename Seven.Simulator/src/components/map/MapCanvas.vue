<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import type { SimMapConnection, SimMapDevice, SimMapEdge, SimMapNode } from '@/lib/project/schema'
import {
  catalogEntry,
  deviceCenter,
  parsePortRef,
  portWorldPosition,
  type DeviceCatalogType,
} from './deviceCatalog'
import { hitTestDevice, hitTestNode, NODE_HEIGHT, NODE_WIDTH } from './hitTest'
import { useMapCamera } from './useMapCamera'

const props = defineProps<{
  nodes: SimMapNode[]
  edges: SimMapEdge[]
  devices: SimMapDevice[]
  connections: SimMapConnection[]
  selectedNodeId?: string | null
  selectedDeviceId?: string | null
  placementType?: DeviceCatalogType | null
}>()

const emit = defineEmits<{
  selectNode: [id: string | null]
  selectDevice: [id: string | null]
  placeDevice: [payload: { type: DeviceCatalogType; x: number; y: number }]
}>()

const containerRef = ref<HTMLDivElement | null>(null)
const canvasRef = ref<HTMLCanvasElement | null>(null)
const { camera, pan, zoomAt, screenToWorld, worldToScreen } = useMapCamera()

let dragging = false
let dragStartX = 0
let dragStartY = 0
let pointerDownX = 0
let pointerDownY = 0
let resizeObserver: ResizeObserver | null = null

const CLICK_THRESHOLD = 4

const hitTestDevices = computed(() =>
  props.devices.flatMap((d) => {
    const entry = catalogEntry(d.type)
    if (!entry) return []
    return [{ id: d.id, x: d.x, y: d.y, width: entry.width, height: entry.height }]
  }),
)

function nodeById(id: string): SimMapNode | undefined {
  return props.nodes.find((n) => n.id === id)
}

function deviceById(id: string): SimMapDevice | undefined {
  return props.devices.find((d) => d.id === id)
}

function resizeCanvas() {
  const container = containerRef.value
  const canvas = canvasRef.value
  if (!container || !canvas) return
  const dpr = window.devicePixelRatio || 1
  const w = container.clientWidth
  const h = container.clientHeight
  canvas.width = Math.max(1, Math.floor(w * dpr))
  canvas.height = Math.max(1, Math.floor(h * dpr))
  canvas.style.width = `${w}px`
  canvas.style.height = `${h}px`
  draw()
}

function drawGrid(ctx: CanvasRenderingContext2D, w: number, h: number) {
  const step = 40 * camera.scale
  if (step < 8) return
  ctx.save()
  ctx.strokeStyle = '#dde4ec'
  ctx.lineWidth = 1
  const startX = camera.offsetX % step
  const startY = camera.offsetY % step
  for (let x = startX; x < w; x += step) {
    ctx.beginPath()
    ctx.moveTo(x, 0)
    ctx.lineTo(x, h)
    ctx.stroke()
  }
  for (let y = startY; y < h; y += step) {
    ctx.beginPath()
    ctx.moveTo(0, y)
    ctx.lineTo(w, y)
    ctx.stroke()
  }
  ctx.restore()
}

function drawDeviceShape(
  ctx: CanvasRenderingContext2D,
  device: SimMapDevice,
  selected: boolean,
) {
  const entry = catalogEntry(device.type)
  if (!entry) return

  const tl = worldToScreen(device.x, device.y)
  const w = entry.width * camera.scale
  const h = entry.height * camera.scale
  const cx = tl.x + w / 2
  const cy = tl.y + h / 2

  ctx.fillStyle = selected ? entry.fill : `${entry.fill}dd`
  ctx.strokeStyle = selected ? '#ffd166' : entry.stroke
  ctx.lineWidth = selected ? 2.5 : 1.5

  ctx.beginPath()
  if (entry.shape === 'circle') {
    ctx.arc(cx, cy, Math.min(w, h) / 2, 0, Math.PI * 2)
  } else if (entry.shape === 'diamond') {
    ctx.moveTo(cx, tl.y)
    ctx.lineTo(tl.x + w, cy)
    ctx.lineTo(cx, tl.y + h)
    ctx.lineTo(tl.x, cy)
    ctx.closePath()
  } else {
    const r = Math.min(8 * camera.scale, w / 4, h / 4)
    ctx.roundRect(tl.x, tl.y, w, h, r)
  }
  ctx.fill()
  ctx.stroke()

  ctx.fillStyle = '#ffffff'
  ctx.font = `${Math.max(9, 11 * camera.scale)}px system-ui, sans-serif`
  ctx.textAlign = 'center'
  ctx.textBaseline = 'middle'
  ctx.fillText(device.code, cx, cy)

  for (const port of entry.ports) {
    const pw = portWorldPosition(device, port.id)
    if (!pw) continue
    const ps = worldToScreen(pw.x, pw.y)
    ctx.beginPath()
    ctx.fillStyle = selected ? '#ffd166' : '#e9ecef'
    ctx.arc(ps.x, ps.y, Math.max(3, 4 * camera.scale), 0, Math.PI * 2)
    ctx.fill()
    ctx.strokeStyle = entry.stroke
    ctx.lineWidth = 1
    ctx.stroke()
  }
}

function draw() {
  const canvas = canvasRef.value
  if (!canvas) return
  const ctx = canvas.getContext('2d')
  if (!ctx) return

  const dpr = window.devicePixelRatio || 1
  const w = canvas.width
  const h = canvas.height
  ctx.setTransform(dpr, 0, 0, dpr, 0, 0)
  ctx.clearRect(0, 0, w / dpr, h / dpr)

  const cssW = w / dpr
  const cssH = h / dpr

  ctx.fillStyle = '#f0f4f8'
  ctx.fillRect(0, 0, cssW, cssH)
  drawGrid(ctx, cssW, cssH)

  ctx.save()
  ctx.strokeStyle = '#8fa3b8'
  ctx.lineWidth = 1.5
  for (const edge of props.edges) {
    const from = nodeById(edge.from)
    const to = nodeById(edge.to)
    if (!from || !to) continue
    const fc = { x: from.x + NODE_WIDTH / 2, y: from.y + NODE_HEIGHT / 2 }
    const tc = { x: to.x + NODE_WIDTH / 2, y: to.y + NODE_HEIGHT / 2 }
    const fs = worldToScreen(fc.x, fc.y)
    const ts = worldToScreen(tc.x, tc.y)
    ctx.beginPath()
    ctx.moveTo(fs.x, fs.y)
    ctx.lineTo(ts.x, ts.y)
    ctx.stroke()
  }
  ctx.restore()

  ctx.save()
  ctx.strokeStyle = '#e76f51'
  ctx.lineWidth = 2
  ctx.setLineDash([6 * camera.scale, 4 * camera.scale])
  for (const conn of props.connections) {
    const fromParsed = parsePortRef(conn.from)
    const toParsed = parsePortRef(conn.to)
    if (!fromParsed || !toParsed) continue
    const fromDev = deviceById(fromParsed.deviceId)
    const toDev = deviceById(toParsed.deviceId)
    if (!fromDev || !toDev) continue
    const fp = portWorldPosition(fromDev, fromParsed.portId) ?? deviceCenter(fromDev)
    const tp = portWorldPosition(toDev, toParsed.portId) ?? deviceCenter(toDev)
    const fs = worldToScreen(fp.x, fp.y)
    const ts = worldToScreen(tp.x, tp.y)
    ctx.beginPath()
    ctx.moveTo(fs.x, fs.y)
    ctx.lineTo(ts.x, ts.y)
    ctx.stroke()
  }
  ctx.setLineDash([])
  ctx.restore()

  for (const node of props.nodes) {
    const selected = node.id === props.selectedNodeId
    const tl = worldToScreen(node.x, node.y)
    const nw = NODE_WIDTH * camera.scale
    const nh = NODE_HEIGHT * camera.scale

    ctx.fillStyle = selected ? '#1a5fb4' : '#0f2744'
    ctx.strokeStyle = selected ? '#62a0ea' : '#3d5a80'
    ctx.lineWidth = selected ? 2 : 1
    const r = Math.min(6 * camera.scale, nw / 4, nh / 4)
    ctx.beginPath()
    ctx.roundRect(tl.x, tl.y, nw, nh, r)
    ctx.fill()
    ctx.stroke()

    ctx.fillStyle = '#ffffff'
    ctx.font = `${Math.max(10, 12 * camera.scale)}px system-ui, sans-serif`
    ctx.textAlign = 'center'
    ctx.textBaseline = 'middle'
    ctx.fillText(node.code, tl.x + nw / 2, tl.y + nh / 2)
  }

  for (const device of props.devices) {
    drawDeviceShape(ctx, device, device.id === props.selectedDeviceId)
  }

  if (!props.nodes.length && !props.devices.length) {
    ctx.fillStyle = '#8899aa'
    ctx.font = '14px system-ui, sans-serif'
    ctx.textAlign = 'center'
    ctx.textBaseline = 'middle'
    const hint = props.placementType
      ? '点击画布放置设备'
      : '添加节点/设备或导入工程'
    ctx.fillText(hint, cssW / 2, cssH / 2)
  }
}

function localPoint(e: MouseEvent): { x: number; y: number } {
  const rect = canvasRef.value!.getBoundingClientRect()
  return { x: e.clientX - rect.left, y: e.clientY - rect.top }
}

function onPointerDown(e: MouseEvent) {
  if (e.button !== 0) return
  dragging = true
  dragStartX = e.clientX
  dragStartY = e.clientY
  pointerDownX = e.clientX
  pointerDownY = e.clientY
}

function onPointerMove(e: MouseEvent) {
  if (!dragging) return
  pan(e.clientX - dragStartX, e.clientY - dragStartY)
  dragStartX = e.clientX
  dragStartY = e.clientY
  draw()
}

function onPointerUp(e: MouseEvent) {
  if (!dragging) return
  dragging = false
  const moved =
    Math.abs(e.clientX - pointerDownX) + Math.abs(e.clientY - pointerDownY)
  if (moved > CLICK_THRESHOLD) return

  const pt = localPoint(e)
  const world = screenToWorld(pt.x, pt.y)

  if (props.placementType) {
    const entry = catalogEntry(props.placementType)
    const w = entry?.width ?? 48
    const h = entry?.height ?? 48
    emit('placeDevice', {
      type: props.placementType,
      x: world.x - w / 2,
      y: world.y - h / 2,
    })
    return
  }

  const deviceHit = hitTestDevice(hitTestDevices.value, world.x, world.y)
  if (deviceHit) {
    emit('selectDevice', deviceHit)
    emit('selectNode', null)
    return
  }

  const nodeHit = hitTestNode(props.nodes, world.x, world.y)
  emit('selectNode', nodeHit)
  emit('selectDevice', null)
}

function onWheel(e: WheelEvent) {
  e.preventDefault()
  const pt = localPoint(e)
  zoomAt(e.deltaY, pt.x, pt.y)
  draw()
}

function onDragOver(e: DragEvent) {
  if (e.dataTransfer?.types.includes('application/x-seven-device')) {
    e.preventDefault()
  }
}

function onDrop(e: DragEvent) {
  const type = e.dataTransfer?.getData('application/x-seven-device') as DeviceCatalogType | ''
  if (!type || !catalogEntry(type)) return
  e.preventDefault()
  const pt = localPoint(e)
  const world = screenToWorld(pt.x, pt.y)
  const entry = catalogEntry(type)!
  emit('placeDevice', {
    type,
    x: world.x - entry.width / 2,
    y: world.y - entry.height / 2,
  })
}

watch(
  () => [
    props.nodes,
    props.edges,
    props.devices,
    props.connections,
    props.selectedNodeId,
    props.selectedDeviceId,
    props.placementType,
    camera.offsetX,
    camera.offsetY,
    camera.scale,
  ],
  () => draw(),
  { deep: true },
)

onMounted(() => {
  resizeCanvas()
  resizeObserver = new ResizeObserver(() => resizeCanvas())
  if (containerRef.value) resizeObserver.observe(containerRef.value)
})

onUnmounted(() => {
  resizeObserver?.disconnect()
})
</script>

<template>
  <div ref="containerRef" class="map-canvas-wrap" :class="{ placing: !!placementType }">
    <canvas
      ref="canvasRef"
      class="map-canvas"
      @mousedown="onPointerDown"
      @mousemove="onPointerMove"
      @mouseup="onPointerUp"
      @mouseleave="dragging = false"
      @wheel.prevent="onWheel"
      @dragover="onDragOver"
      @drop="onDrop"
    />
  </div>
</template>

<style scoped>
.map-canvas-wrap {
  position: relative;
  width: 100%;
  min-height: 360px;
  height: 420px;
  border: 1px solid #d5dde6;
  border-radius: 8px;
  overflow: hidden;
  background: #f0f4f8;
}
.map-canvas-wrap.placing {
  border-color: #e76f51;
  box-shadow: inset 0 0 0 1px #e76f5144;
}
.map-canvas {
  display: block;
  width: 100%;
  height: 100%;
  cursor: grab;
}
.map-canvas-wrap.placing .map-canvas {
  cursor: crosshair;
}
.map-canvas:active {
  cursor: grabbing;
}
.map-canvas-wrap.placing .map-canvas:active {
  cursor: crosshair;
}
</style>
