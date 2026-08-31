<script setup lang="ts">
import { onMounted, onUnmounted, ref, watch } from 'vue'
import * as THREE from 'three'
import { OrbitControls } from 'three/addons/controls/OrbitControls.js'
import { catalogEntry, deviceCenter } from '@/components/map/deviceCatalog'
import { NODE_HEIGHT, NODE_WIDTH } from '@/components/map/hitTest'
import type { SimMapDevice, SimMapNode } from '@/lib/project/schema'

const props = defineProps<{
  nodes: SimMapNode[]
  devices: SimMapDevice[]
}>()

const containerRef = ref<HTMLDivElement | null>(null)

const SCALE = 0.01

let renderer: THREE.WebGLRenderer | null = null
let scene: THREE.Scene | null = null
let camera: THREE.PerspectiveCamera | null = null
let controls: OrbitControls | null = null
let animationId = 0
let nodeGroup: THREE.Group | null = null
let deviceGroup: THREE.Group | null = null
const deviceMeshes = new Map<string, THREE.Mesh>()
let nodeMaterial: THREE.MeshStandardMaterial | null = null
let deviceMaterial: THREE.MeshStandardMaterial | null = null
let resizeObserver: ResizeObserver | null = null

function clearGroup(group: THREE.Group) {
  while (group.children.length > 0) {
    const obj = group.children[0]!
    group.remove(obj)
    if (obj instanceof THREE.Mesh) {
      obj.geometry.dispose()
    }
  }
}

function buildMeshes() {
  if (!nodeGroup || !deviceGroup || !nodeMaterial || !deviceMaterial) return

  clearGroup(nodeGroup)
  clearGroup(deviceGroup)
  deviceMeshes.clear()

  for (const node of props.nodes) {
    const cx = (node.x + NODE_WIDTH / 2) * SCALE
    const cz = (node.y + NODE_HEIGHT / 2) * SCALE
    const geo = new THREE.BoxGeometry(NODE_WIDTH * SCALE, 0.24, NODE_HEIGHT * SCALE)
    const mesh = new THREE.Mesh(geo, nodeMaterial)
    mesh.position.set(cx, 0.12, cz)
    nodeGroup.add(mesh)
  }

  for (const device of props.devices) {
    const entry = catalogEntry(device.type)
    const w = (entry?.width ?? 48) * SCALE
    const h = (entry?.height ?? 48) * SCALE
    const center = deviceCenter(device)
    const geo = new THREE.BoxGeometry(w, Math.max(h, w * 0.6), h)
    const mesh = new THREE.Mesh(geo, deviceMaterial)
    mesh.position.set(center.x * SCALE, Math.max(h, w * 0.6) / 2, center.y * SCALE)
    deviceGroup.add(mesh)
    deviceMeshes.set(device.code, mesh)
  }

  fitCamera()
}

function fitCamera() {
  if (!camera || !controls) return

  const box = new THREE.Box3()
  if (nodeGroup) box.expandByObject(nodeGroup)
  if (deviceGroup) box.expandByObject(deviceGroup)

  if (box.isEmpty()) {
    camera.position.set(4, 6, 8)
    controls.target.set(0, 0, 0)
    controls.update()
    return
  }

  const center = box.getCenter(new THREE.Vector3())
  const size = box.getSize(new THREE.Vector3())
  const maxDim = Math.max(size.x, size.y, size.z, 1)
  const dist = maxDim * 2.2

  camera.position.set(center.x + dist * 0.6, center.y + dist * 0.8, center.z + dist * 0.6)
  controls.target.copy(center)
  controls.update()
}

function resize() {
  const container = containerRef.value
  if (!container || !renderer || !camera) return

  const w = Math.max(container.clientWidth, 1)
  const h = Math.max(container.clientHeight, 1)
  camera.aspect = w / h
  camera.updateProjectionMatrix()
  renderer.setSize(w, h)
}

function animate() {
  animationId = requestAnimationFrame(animate)
  controls?.update()
  if (renderer && scene && camera) {
    renderer.render(scene, camera)
  }
}

function dispose() {
  cancelAnimationFrame(animationId)
  resizeObserver?.disconnect()
  resizeObserver = null

  if (nodeGroup) clearGroup(nodeGroup)
  if (deviceGroup) clearGroup(deviceGroup)

  nodeMaterial?.dispose()
  deviceMaterial?.dispose()
  nodeMaterial = null
  deviceMaterial = null

  controls?.dispose()
  controls = null

  renderer?.dispose()
  if (renderer?.domElement.parentElement) {
    renderer.domElement.parentElement.removeChild(renderer.domElement)
  }
  renderer = null

  scene = null
  camera = null
  nodeGroup = null
  deviceGroup = null
  deviceMeshes.clear()
}

/** Nudge a device mesh slightly on inbound Gateway traffic (visual feedback). */
function nudgeDevice(deviceCode?: string) {
  const mesh =
    (deviceCode ? deviceMeshes.get(deviceCode) : undefined) ??
    deviceMeshes.values().next().value
  if (!mesh) return

  const baseY = mesh.position.y
  mesh.position.y = baseY + 0.35
  window.setTimeout(() => {
    mesh.position.y = baseY
  }, 280)
}

defineExpose({ nudgeDevice })

function init() {
  const container = containerRef.value
  if (!container) return

  scene = new THREE.Scene()
  scene.background = new THREE.Color(0xf0f4f8)

  const w = Math.max(container.clientWidth, 1)
  const h = Math.max(container.clientHeight, 1)
  camera = new THREE.PerspectiveCamera(50, w / h, 0.01, 500)
  camera.position.set(4, 6, 8)

  renderer = new THREE.WebGLRenderer({ antialias: true })
  renderer.setPixelRatio(window.devicePixelRatio)
  renderer.setSize(w, h)
  container.appendChild(renderer.domElement)

  controls = new OrbitControls(camera, renderer.domElement)
  controls.enableDamping = true
  controls.dampingFactor = 0.08

  scene.add(new THREE.AmbientLight(0xffffff, 0.65))
  const dir = new THREE.DirectionalLight(0xffffff, 0.85)
  dir.position.set(8, 16, 6)
  scene.add(dir)

  const grid = new THREE.GridHelper(24, 24, 0xbfc9d4, 0xdce3ea)
  scene.add(grid)

  nodeMaterial = new THREE.MeshStandardMaterial({ color: 0x4a90d9 })
  deviceMaterial = new THREE.MeshStandardMaterial({ color: 0xe67e22 })

  nodeGroup = new THREE.Group()
  deviceGroup = new THREE.Group()
  scene.add(nodeGroup, deviceGroup)

  buildMeshes()
  animate()

  resizeObserver = new ResizeObserver(resize)
  resizeObserver.observe(container)
}

watch(
  () => [props.nodes, props.devices] as const,
  () => buildMeshes(),
  { deep: true },
)

onMounted(init)
onUnmounted(dispose)
</script>

<template>
  <div ref="containerRef" class="three-scene" />
</template>

<style scoped>
.three-scene {
  width: 100%;
  height: 360px;
  min-height: 240px;
  border-radius: 6px;
  overflow: hidden;
  background: #f0f4f8;
}
</style>
